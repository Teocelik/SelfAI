/**
 * Model Picker Module (F.M.5 — dinamik catalog, tam ekran modal)
 *
 * Eski model-selection-panel.js (side panel + hardcoded kartlar) yerine gelir.
 * Modeller /Catalog/Image endpoint'inden dinamik gelir; Affogato benzeri tam
 * ekran modal'da search + filter + favori ile listelenir.
 *
 * ENTEGRASYON: Seçim, mevcut #selectedModelValue hidden input'una yazılır ve
 * 'model-selection-changed' event'i dispatch edilir — böylece app.js (generate
 * payload) ve generate-button-state.js HİÇ değişmeden çalışmaya devam eder.
 */

const ModelPicker = (function () {
    'use strict';

    // DOM
    let modal = null;
    let trigger = null;
    let backdrop = null;
    let searchInput = null;
    let gridContainer = null;
    let countEl = null;
    let loadingEl = null;
    let emptyEl = null;

    // Hidden input (backend'e giden Model değeri — tek kaynak)
    let selectedModelInput = null;

    // State
    let currentFilter = 'all';
    let allModels = [];          // /Catalog/Image'ten gelen liste
    let selectedEndpointId = null;
    let isLoaded = false;

    function init() {
        modal = document.getElementById('modelPickerModal');
        trigger = document.getElementById('modelPickerTrigger');
        backdrop = document.getElementById('modelPickerBackdrop');
        searchInput = document.getElementById('modelPickerSearch');
        gridContainer = document.getElementById('modelPickerGrid');
        countEl = document.getElementById('modelPickerCount');
        loadingEl = document.getElementById('modelPickerLoading');
        emptyEl = document.getElementById('modelPickerEmpty');
        selectedModelInput = document.getElementById('selectedModelValue');

        if (!modal || !trigger) {
            console.warn('[ModelPicker] Gerekli element bulunamadı (modal/trigger)');
            return;
        }

        trigger.addEventListener('click', openModal);
        document.getElementById('modelPickerClose')?.addEventListener('click', closeModal);
        backdrop?.addEventListener('click', closeModal);
        searchInput?.addEventListener('input', renderModels);
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && modal && !modal.hidden) closeModal();
        });

        modal.querySelectorAll('.model-picker-modal__filter').forEach(btn => {
            btn.addEventListener('click', () => setFilter(btn.dataset.filter, btn));
        });

        // Character/Face aktifken trigger'da override rozeti göster.
        document.addEventListener('feature-mutex-changed', function (e) {
            updateTriggerOverride(e.detail ? e.detail.active : null);
        });

        // Sayfa açılışında catalog'u önceden çek (modal ilk açılışta hazır olsun).
        loadCatalog();
    }

    /**
     * Feature (character/face) aktifken model seçiminin otomatik override edildiğini
     * trigger butonunda rozetle gösterir. Bilinmeyen feature'da rozet render edilmez.
     */
    function updateTriggerOverride(activeFeature) {
        if (!trigger) return;
        const existing = trigger.querySelector('.model-picker-trigger__override-badge');

        if (!activeFeature) {
            if (existing) existing.remove();
            trigger.classList.remove('is-overridden');
            return;
        }

        let badgeText;
        switch (activeFeature) {
            case 'character':
                badgeText = 'Otomatik: Flux LoRA';
                break;
            case 'face':
                badgeText = 'Otomatik: PuLID Flux';
                break;
            default:
                // Bilinmeyen feature → rozet gösterme
                if (existing) existing.remove();
                trigger.classList.remove('is-overridden');
                return;
        }

        trigger.classList.add('is-overridden');

        let badgeEl = existing;
        if (!badgeEl) {
            badgeEl = document.createElement('div');
            badgeEl.className = 'model-picker-trigger__override-badge';
            trigger.appendChild(badgeEl);
        }
        badgeEl.textContent = badgeText;
    }

    async function loadCatalog() {
        if (loadingEl) loadingEl.hidden = false;
        try {
            const response = await fetch('/Catalog/Image', {
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error('HTTP ' + response.status);

            const json = await response.json();
            const data = json.data || json.Data || json;
            allModels = data.models || data.Models || [];
            isLoaded = true;
            renderModels();
        } catch (err) {
            console.error('[ModelPicker] Catalog yükleme hatası:', err);
            if (typeof Toast !== 'undefined') Toast.error('Modeller yüklenemedi.', 'Hata');
        } finally {
            if (loadingEl) loadingEl.hidden = true;
        }
    }

    function renderModels() {
        if (!gridContainer) return;
        gridContainer.innerHTML = '';

        const filtered = applyFilters(allModels);
        if (countEl) {
            countEl.textContent = `${allModels.length} model · ${filtered.length} eşleşme`;
        }

        if (emptyEl) emptyEl.hidden = filtered.length !== 0;

        filtered.forEach(model => gridContainer.appendChild(createModelCard(model)));
    }

    function applyFilters(models) {
        const term = (searchInput?.value || '').toLowerCase().trim();
        return models.filter(m => {
            if (currentFilter === 'recommended' && !m.isRecommended) return false;
            if (currentFilter === 'favorites' && !m.isFavorited) return false;
            if (['Fast', 'Standard', 'Premium'].includes(currentFilter) && m.tier !== currentFilter) return false;

            if (term) {
                const hay = `${m.displayName || ''} ${m.provider || ''} ${m.description || ''}`.toLowerCase();
                if (!hay.includes(term)) return false;
            }
            return true;
        });
    }

    function createModelCard(model) {
        const card = document.createElement('div');
        card.className = 'model-picker-card';
        card.dataset.endpointId = model.endpointId;
        if (model.isRecommended) card.classList.add('is-recommended');
        if (model.endpointId === selectedEndpointId) card.classList.add('is-selected');

        // 1. Thumbnail (full bleed)
        if (model.thumbnailUrl) {
            const img = document.createElement('img');
            img.className = 'model-picker-card__thumbnail';
            img.src = model.thumbnailUrl;
            img.alt = model.displayName || '';
            img.loading = 'lazy';
            // Broken/erişilemeyen URL → img'i gizle, yedek placeholder ekle.
            img.addEventListener('error', () => {
                img.classList.add('is-broken');
                if (!card.querySelector('.model-picker-card__placeholder')) {
                    card.insertBefore(buildPlaceholder(), card.firstChild);
                }
            });
            card.appendChild(img);
        } else {
            card.appendChild(buildPlaceholder());
        }

        // 2. Recommended badge (sol üst)
        if (model.isRecommended) {
            const badge = document.createElement('div');
            badge.className = 'model-picker-card__badge';
            badge.textContent = 'Önerilen';
            card.appendChild(badge);
        }

        // 3. Favorite star (sağ üst)
        const favBtn = document.createElement('button');
        favBtn.type = 'button';
        favBtn.className = 'model-picker-card__favorite' + (model.isFavorited ? ' is-active' : '');
        favBtn.setAttribute('aria-label', 'Favorilere ekle');
        favBtn.textContent = model.isFavorited ? '★' : '☆';
        favBtn.addEventListener('click', (e) => {
            e.stopPropagation();  // Kart click'i tetiklenmesin
            toggleFavorite(model.endpointId);
        });
        card.appendChild(favBtn);

        // 4. Bottom content (name + provider + credit) — overlay
        const content = document.createElement('div');
        content.className = 'model-picker-card__content';

        const name = document.createElement('h3');
        name.className = 'model-picker-card__name';
        name.textContent = model.displayName || model.endpointId;
        content.appendChild(name);

        const meta = document.createElement('div');
        meta.className = 'model-picker-card__meta';

        const provider = document.createElement('span');
        provider.className = 'model-picker-card__provider';
        provider.textContent = model.provider || '';
        meta.appendChild(provider);

        const credit = document.createElement('span');
        credit.className = 'model-picker-card__credit';
        credit.textContent = `${model.creditCost} kredi`;
        meta.appendChild(credit);

        content.appendChild(meta);
        card.appendChild(content);

        // Card click → select model
        card.addEventListener('click', () => {
            selectModel(model);
            closeModal();
        });

        return card;
    }

    function buildPlaceholder() {
        const placeholder = document.createElement('div');
        placeholder.className = 'model-picker-card__placeholder';
        placeholder.textContent = '🎨';
        return placeholder;
    }

    function selectModel(model) {
        selectedEndpointId = model.endpointId;

        // 1) Tek kaynak: hidden input (app.js + generate-button-state.js bunu okur)
        if (selectedModelInput) selectedModelInput.value = model.endpointId;

        // 2) Trigger preview
        const nameEl = document.getElementById('mpSelectedName');
        const creditEl = document.getElementById('mpSelectedCredit');
        const thumbEl = document.getElementById('mpSelectedThumbnail');
        if (nameEl) nameEl.textContent = model.displayName || model.endpointId;
        if (creditEl) creditEl.textContent = `${model.creditCost} kredi`;
        if (thumbEl) {
            if (model.thumbnailUrl) {
                thumbEl.src = model.thumbnailUrl;
                thumbEl.hidden = false;
            } else {
                thumbEl.hidden = true;
                thumbEl.removeAttribute('src');
            }
        }

        // 3) Seçim değişimini yayınla (generate-button-state.js dinler)
        document.dispatchEvent(new CustomEvent('model-selection-changed', {
            detail: { selectedEndpoint: model.endpointId, model }
        }));

        // 4) Defansif: buton state'ini tazele
        if (window.GenerateButtonState) window.GenerateButtonState.updateState();
    }

    async function toggleFavorite(endpointId) {
        try {
            const response = await fetch('/Catalog/ToggleFavorite', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ endpointId })
            });
            if (!response.ok) throw new Error('HTTP ' + response.status);

            const json = await response.json();
            const model = allModels.find(m => m.endpointId === endpointId);
            if (model) {
                // Sunucu yeni durumu döner; yoksa local toggle
                model.isFavorited = (typeof json.isFavorited === 'boolean')
                    ? json.isFavorited
                    : !model.isFavorited;
            }
            renderModels();
        } catch (err) {
            console.error('[ModelPicker] Favori toggle hatası:', err);
            if (typeof Toast !== 'undefined') Toast.error('Favori güncellenemedi.', 'Hata');
        }
    }

    function setFilter(filter, btnEl) {
        currentFilter = filter;
        modal.querySelectorAll('.model-picker-modal__filter')
            .forEach(b => b.classList.remove('is-active'));
        btnEl.classList.add('is-active');
        renderModels();
    }

    function openModal() {
        if (!modal) return;
        modal.hidden = false;
        document.body.classList.add('is-model-picker-open');
        if (!isLoaded) loadCatalog();
        searchInput?.focus();
    }

    function closeModal() {
        if (!modal) return;
        modal.hidden = true;
        document.body.classList.remove('is-model-picker-open');
    }

    function getSelectedEndpoint() {
        return selectedEndpointId;
    }

    function reset() {
        selectedEndpointId = null;
        if (selectedModelInput) selectedModelInput.value = '';

        const nameEl = document.getElementById('mpSelectedName');
        const creditEl = document.getElementById('mpSelectedCredit');
        const thumbEl = document.getElementById('mpSelectedThumbnail');
        if (nameEl) nameEl.textContent = 'Model seç';
        if (creditEl) creditEl.textContent = '';
        if (thumbEl) { thumbEl.hidden = true; thumbEl.removeAttribute('src'); }

        document.dispatchEvent(new CustomEvent('model-selection-changed', {
            detail: { selectedEndpoint: null }
        }));

        closeModal();
    }

    // Public API
    return { init, getSelectedEndpoint, reset };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModelPicker;
}

// Global erişim (app.js wiring + diğer modüller için)
window.ModelPicker = ModelPicker;
