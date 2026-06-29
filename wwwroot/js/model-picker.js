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

        // Sayfa açılışında catalog'u önceden çek (modal ilk açılışta hazır olsun).
        loadCatalog();
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
        card.className = 'model-card-pick';
        card.dataset.endpointId = model.endpointId;

        // Media
        const media = document.createElement('div');
        media.className = 'model-card-pick__media';
        if (model.thumbnailUrl) {
            const img = document.createElement('img');
            img.src = model.thumbnailUrl;
            img.alt = model.displayName || '';
            img.loading = 'lazy';
            // Broken/erişilemeyen URL → img'i kaldır, fallback ikonunu göster.
            img.addEventListener('error', () => {
                img.remove();
                media.prepend(buildMediaFallback());
            });
            media.appendChild(img);
        } else {
            media.appendChild(buildMediaFallback());
        }

        if (model.isRecommended) {
            const badge = document.createElement('span');
            badge.className = 'model-card-pick__badge';
            badge.textContent = 'Önerilen';
            media.appendChild(badge);
        }

        // Favori yıldızı
        const fav = document.createElement('button');
        fav.type = 'button';
        fav.className = 'model-card-pick__fav' + (model.isFavorited ? ' is-favorited' : '');
        fav.setAttribute('aria-label', 'Favori');
        fav.innerHTML = `<i class="fa${model.isFavorited ? 's' : 'r'} fa-star"></i>`;
        fav.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleFavorite(model.endpointId);
        });
        media.appendChild(fav);

        // Info
        const info = document.createElement('div');
        info.className = 'model-card-pick__info';
        info.innerHTML = `
            <span class="model-card-pick__name">${escapeHtml(model.displayName || model.endpointId)}</span>
            <span class="model-card-pick__provider">${escapeHtml(model.provider || '')}</span>
            <span class="model-card-pick__credit">${model.creditCost} kredi</span>
        `;

        card.appendChild(media);
        card.appendChild(info);

        card.addEventListener('click', () => {
            selectModel(model);
            closeModal();
        });

        return card;
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

    function buildMediaFallback() {
        const fallback = document.createElement('div');
        fallback.className = 'model-card-pick__media-fallback';
        fallback.innerHTML = '<i class="fas fa-image"></i>';
        return fallback;
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str == null ? '' : String(str);
        return div.innerHTML;
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
