/**
 * Character Modal Module (F.5c + F.6.2 + F.6.3)
 * Full-screen karakter seçim modal'ını yönetir: aç/kapat, client-side search,
 * mode toggle ve kart seçimi (F.5c). F.6.2 ile sub-modal view swap (grid ↔ create)
 * ve "+ Yeni Karakter" formu eklendi. F.6.3 ile grid artık MOCK değil — modal her
 * açılışta /Characters/List endpoint'inden taze çekilir ve kartlar dinamik render
 * edilir. Oluşturulan karakter de başarılı submit sonrası grid'e dahil olur.
 *
 * ÖNEMLİ: Karakter seçme, mode set, @Name prompt yönetimi, Face Lock mutual
 * exclusivity ve buton thumbnail gibi TÜM seçim iş mantığı character-panel.js'te
 * yaşar ve buradan public API ile çağrılır (window.CharacterPanel.selectCharacter /
 * setMode / clearCharacter). Yeniden yazım yok.
 *
 * NOT (backend response): /Characters/List, MVC konvansiyonu gereği
 * { success, message, data: { items, totalCount } } sarmalıyla döner — kart
 * listesi payload.data.items içinde gelir.
 */
(function () {
    'use strict';

    let modal = null;
    let searchInput = null;
    let cards = [];                 // Dinamik liste — render sonrası güncellenir
    let emptySearchState = null;    // Search match yok (F.5c)
    let emptyListState = null;      // Hiç karakter yok (F.6.3)
    let loadingState = null;        // Fetch sırasında spinner
    let gridContainer = null;
    let currentAssetId = null;

    document.addEventListener('DOMContentLoaded', function () {
        modal = document.getElementById('characterModal');
        if (!modal) return;

        searchInput = document.getElementById('characterSearchInput');
        gridContainer = document.getElementById('characterGrid');
        emptySearchState = document.getElementById('characterEmpty');
        emptyListState = document.getElementById('characterEmptyList');
        loadingState = document.getElementById('characterLoading');

        // Character butonu — Studio Consistency Controls'taki tool button
        const characterBtn = document.getElementById('characterBtn')
                          || document.querySelector('[data-character-trigger]');
        if (characterBtn) {
            characterBtn.addEventListener('click', function (e) {
                e.preventDefault();
                openModal();
            });
        }

        // Close handler'ları (backdrop + X butonu)
        document.querySelectorAll('[data-character-modal-close]').forEach(function (el) {
            el.addEventListener('click', closeModal);
        });

        // ESC ile kapat
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && modal.classList.contains('is-open')) {
                closeModal();
            }
        });

        // Search (client-side filtering by name) — dinamik kartlar üzerinde çalışır
        if (searchInput) {
            searchInput.addEventListener('input', handleSearch);
        }

        // Mode toggle (Esnek / Dengeli / Güçlü) — statik markup, grid dışı.
        // character-panel.js'in mode mantığını çağırır.
        modal.querySelectorAll('.character-mode-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const mode = btn.dataset.mode;
                if (window.CharacterPanel && typeof window.CharacterPanel.setMode === 'function') {
                    window.CharacterPanel.setMode(mode);
                }
            });
        });

        // Empty list CTA ("Yeni Karakter Oluştur") → create view'a geç (F.6.3)
        const emptyListCreateBtn = document.getElementById('characterEmptyListCreate');
        if (emptyListCreateBtn) {
            emptyListCreateBtn.addEventListener('click', function () {
                switchToView('create');
            });
        }

        // F.6.2 — grid ↔ create view geçişleri ve oluşturma formu
        initViewSwap();
        initCreationForm();
    });

    async function openModal() {
        if (!modal) return;
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('is-character-modal-open');

        // Açılış her zaman grid view'da başlar (create view'da kalmışsa sıfırla)
        switchToView('grid');

        // Karakterleri her açılışta taze çek (cache yok)
        await loadCharacters();

        setTimeout(function () {
            if (searchInput && cards.length > 0) searchInput.focus();
        }, 100);
    }

    function closeModal() {
        if (!modal) return;
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('is-character-modal-open');

        // Search'i temizle, gizli kartları geri göster
        if (searchInput) searchInput.value = '';
        cards.forEach(function (c) { c.classList.remove('is-hidden'); });
        if (emptySearchState) emptySearchState.hidden = true;

        // F.6.2 — create view açıksa grid'e reset et (sonraki açılış grid'de başlar).
        resetCreationForm();
        switchToView('grid');
    }

    /* ────────────────────────────────────────────────────────────────────────
       F.6.3 — Karakter listesini fetch + dinamik render
       ──────────────────────────────────────────────────────────────────────── */

    async function loadCharacters() {
        if (!gridContainer) return;

        // Loading state — diğer state'leri gizle
        gridContainer.hidden = true;
        if (emptySearchState) emptySearchState.hidden = true;
        if (emptyListState) emptyListState.hidden = true;
        if (loadingState) loadingState.hidden = false;

        try {
            const response = await fetch('/Characters/List?page=1&pageSize=50', {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });

            if (!response.ok) {
                throw new Error('Karakter listesi yüklenemedi (HTTP ' + response.status + ')');
            }

            const payload = await response.json();
            // Backend sarmalı: { success, message, data: { items, totalCount } }
            const items = (payload && payload.data && payload.data.items) || [];

            if (loadingState) loadingState.hidden = true;

            if (items.length === 0) {
                // Hiç karakter yok → empty list state
                gridContainer.hidden = true;
                if (emptyListState) emptyListState.hidden = false;
                cards = [];
            } else {
                renderCharacters(items);
                gridContainer.hidden = false;
                if (emptyListState) emptyListState.hidden = true;
            }

        } catch (err) {
            console.error('Karakter listesi yükleme hatası:', err);
            if (loadingState) loadingState.hidden = true;

            // Hata fallback → empty list göster (kullanıcı yine de "Yeni Karakter" yapabilsin)
            if (emptyListState) emptyListState.hidden = false;
            gridContainer.hidden = true;
            cards = [];

            if (typeof Toast !== 'undefined' && Toast.error) {
                Toast.error('Karakterler yüklenemedi: ' + err.message);
            }
        }
    }

    function renderCharacters(items) {
        if (!gridContainer) return;

        gridContainer.innerHTML = '';

        const selectedId = getCurrentSelectedCharacterId();

        items.forEach(function (character) {
            const card = createCharacterCard(character, selectedId);
            gridContainer.appendChild(card);
        });

        // cards listesini güncelle (search filtresi bunun üzerinde çalışır)
        cards = Array.from(gridContainer.querySelectorAll('.character-card'));

        // Kart tıklama handler'ları
        cards.forEach(function (card) {
            card.addEventListener('click', function () {
                handleCardClick(card);
            });
        });

        // Broken image → placeholder göster
        gridContainer.querySelectorAll('.character-card__image img').forEach(function (img) {
            img.addEventListener('error', function () {
                img.classList.add('is-broken');
                img.style.display = 'none';
            });
        });
    }

    function createCharacterCard(character, currentSelectedId) {
        const card = document.createElement('button');
        card.type = 'button';
        card.className = 'character-card';
        card.dataset.characterId = character.id;
        card.dataset.characterName = character.name;

        if (currentSelectedId && character.id === currentSelectedId) {
            card.classList.add('is-selected');
        }

        const imageWrap = document.createElement('div');
        imageWrap.className = 'character-card__image';

        if (character.thumbnailUrl) {
            const img = document.createElement('img');
            img.src = character.thumbnailUrl;
            img.alt = character.name;
            img.loading = 'lazy';
            imageWrap.appendChild(img);
        }

        // Placeholder her zaman eklenir; görsel başarıyla yüklenince CSS ile gizlenir
        // (.character-card__image img:not(.is-broken) ~ .character-card__placeholder).
        const placeholder = document.createElement('div');
        placeholder.className = 'character-card__placeholder';
        placeholder.innerHTML = '<i class="fas fa-user"></i>';
        imageWrap.appendChild(placeholder);

        const nameSpan = document.createElement('span');
        nameSpan.className = 'character-card__name';
        nameSpan.textContent = character.name;

        card.appendChild(imageWrap);
        card.appendChild(nameSpan);

        return card;
    }

    /**
     * Seçili karakter ID'sini okur. NOT: Mevcut mimaride seçim character-panel.js'in
     * modül-içi state'inde tutulur (DOM hidden input yok), bu yüzden bu fonksiyon
     * defansiftir — input bulunmazsa null döner (preselection highlight olmaz).
     * Hidden input/persisted selection eklenirse bu okuma otomatik devreye girer.
     */
    function getCurrentSelectedCharacterId() {
        const hiddenInput = document.getElementById('characterIdInput')
                         || document.querySelector('[name="CharacterId"]');
        return hiddenInput ? hiddenInput.value : null;
    }

    function handleSearch(e) {
        const term = e.target.value.toLowerCase().trim();
        let visibleCount = 0;

        cards.forEach(function (card) {
            const name = (card.dataset.characterName || '').toLowerCase();
            const match = name.includes(term);
            card.classList.toggle('is-hidden', !match);
            if (match) visibleCount++;
        });

        // "Bu isimde karakter bulunamadı" — yalnızca kart varken ve filtre boşaltınca
        if (emptySearchState) {
            emptySearchState.hidden = visibleCount > 0 || cards.length === 0;
        }
    }

    function handleCardClick(card) {
        // Re-click to deselect: kart zaten seçiliyse → CLEAR (toggle off)
        if (card.classList.contains('is-selected')) {
            card.classList.remove('is-selected');

            // character-panel.js'in MEVCUT temizleme akışını çağır (yeniden yazma yok)
            if (window.CharacterPanel && typeof window.CharacterPanel.clearCharacter === 'function') {
                window.CharacterPanel.clearCharacter();
            }

            if (typeof Toast !== 'undefined' && Toast.info) {
                Toast.info('Karakter seçimi temizlendi.');
            }

            // Modal AÇIK kalır — kullanıcı başka karakter seçebilir veya kapatabilir
            return;
        }

        // Yeni seçim — görsel selected state (modal-local)
        cards.forEach(function (c) { c.classList.remove('is-selected'); });
        card.classList.add('is-selected');

        const characterId = card.dataset.characterId;
        const characterName = card.dataset.characterName;
        const imgEl = card.querySelector('.character-card__image img');
        const characterImg = imgEl ? imgEl.src : null;

        // character-panel.js'in MEVCUT seçim mantığını çağır (yeniden yazma yok)
        if (window.CharacterPanel && typeof window.CharacterPanel.selectCharacter === 'function') {
            window.CharacterPanel.selectCharacter({
                id: characterId,
                name: characterName,
                imageUrl: characterImg
            });
        }

        if (typeof Toast !== 'undefined' && Toast.success) {
            Toast.success('"' + characterName + '" karakteri seçildi.');
        }

        // Modal'ı kapat
        setTimeout(closeModal, 300);
    }

    /* ────────────────────────────────────────────────────────────────────────
       F.6.2 — Sub-modal view swap (grid ↔ create)
       ──────────────────────────────────────────────────────────────────────── */

    function initViewSwap() {
        const newBtn = document.getElementById('newCharacterBtn');
        const backBtn = document.getElementById('characterCreateBack');
        const cancelBtn = document.getElementById('characterCreateCancel');

        if (newBtn) {
            newBtn.addEventListener('click', function () {
                switchToView('create');
            });
        }

        if (backBtn) {
            backBtn.addEventListener('click', function () {
                switchToView('grid');
            });
        }

        if (cancelBtn) {
            cancelBtn.addEventListener('click', function () {
                if (confirm('Form temizlenecek. Devam edilsin mi?')) {
                    resetCreationForm();
                    switchToView('grid');
                }
            });
        }
    }

    function switchToView(viewName) {
        if (!modal) return;

        modal.setAttribute('data-view', viewName);

        // View div'leri
        modal.querySelectorAll('.character-modal__view').forEach(function (view) {
            const matches = view.getAttribute('data-view') === viewName;
            view.hidden = !matches;
        });

        // Title span'leri
        modal.querySelectorAll('[data-view-title]').forEach(function (span) {
            const matches = span.getAttribute('data-view-title') === viewName;
            span.hidden = !matches;
        });

        // View-only öğeler (örn. "+ Yeni Karakter" butonu)
        modal.querySelectorAll('[data-view-only]').forEach(function (el) {
            const matches = el.getAttribute('data-view-only') === viewName;
            el.hidden = !matches;
        });

        // Aktif view'ın scroll'unu en üste al
        const scroll = modal.querySelector(
            '.character-modal__view[data-view="' + viewName + '"] .character-modal__scroll');
        if (scroll) scroll.scrollTop = 0;
    }

    /* ────────────────────────────────────────────────────────────────────────
       F.6.2 — Karakter oluşturma formu
       ──────────────────────────────────────────────────────────────────────── */

    function initCreationForm() {
        const form = document.getElementById('characterCreateForm');
        const uploadZone = document.getElementById('characterUploadZone');
        const fileInput = document.getElementById('characterFaceInput');
        const previewImg = document.getElementById('characterPreviewImg');
        const previewWrap = document.getElementById('characterUploadPreview');
        const emptyWrap = document.getElementById('characterUploadEmpty');
        const removeBtn = document.getElementById('characterPreviewRemove');
        const assetIdInput = document.getElementById('characterAssetIdInput');

        if (!form) return;

        // Upload zone tıklaması → dosya seçici (remove butonu hariç)
        uploadZone.addEventListener('click', function (e) {
            if (e.target.closest('.character-create-form__upload-remove')) return;
            fileInput.click();
        });

        // Dosya seçildi → önizleme + asset upload
        fileInput.addEventListener('change', async function (e) {
            const file = e.target.files[0];
            if (!file) return;

            // Boyut kontrolü (20MB)
            if (file.size > 20 * 1024 * 1024) {
                showToast('error', 'Dosya boyutu 20MB\'dan büyük olamaz.');
                fileInput.value = '';
                return;
            }

            // Yerel önizleme göster
            const reader = new FileReader();
            reader.onload = function (ev) {
                previewImg.src = ev.target.result;
                previewWrap.hidden = false;
                emptyWrap.hidden = true;
            };
            reader.readAsDataURL(file);

            // Asset upload (mevcut /RenderNet/GetAssetId reuse — face-lock ile aynı pattern).
            // Endpoint IFormFile parametresi 'formFile' adında olduğu için field adı 'formFile'.
            try {
                uploadZone.classList.add('is-loading');

                const formData = new FormData();
                formData.append('formFile', file);

                const response = await fetch('/RenderNet/GetAssetId', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) throw new Error('Asset upload başarısız (HTTP ' + response.status + ')');

                const data = await response.json();
                // Backend yanıtı: { success, assetId, data }
                if (!data.success || !data.assetId) {
                    throw new Error(data.message || 'Asset ID alınamadı');
                }

                currentAssetId = data.assetId;
                if (assetIdInput) assetIdInput.value = currentAssetId;

            } catch (err) {
                console.error('Asset upload hatası:', err);
                showToast('error', 'Yüz görseli yüklenemedi: ' + err.message);
                resetUpload();
            } finally {
                uploadZone.classList.remove('is-loading');
            }
        });

        // Önizlemeyi kaldır
        removeBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            resetUpload();
        });

        function resetUpload() {
            fileInput.value = '';
            previewImg.src = '';
            previewWrap.hidden = true;
            emptyWrap.hidden = false;
            currentAssetId = null;
            if (assetIdInput) assetIdInput.value = '';
        }

        // Form submit
        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            await handleCreateSubmit();
        });
    }

    async function handleCreateSubmit() {
        const nameInput = document.getElementById('characterNameInput');
        const promptInput = document.getElementById('characterPromptInput');
        const submitBtn = document.getElementById('characterCreateSubmit');
        const spinner = submitBtn.querySelector('.character-create-form__submit-spinner');
        const label = submitBtn.querySelector('.character-create-form__submit-label');

        const name = nameInput.value.trim();
        const prompt = promptInput.value.trim();
        const checkedType = document.querySelector('input[name="characterType"]:checked');
        const characterType = checkedType ? checkedType.value : 'realistic';

        // Client-side validation
        if (!currentAssetId) {
            showToast('error', 'Lütfen bir yüz görseli yükle.');
            return;
        }
        if (name.length < 1) {
            showToast('error', 'Karakter ismi gerekli.');
            return;
        }
        if (prompt.length < 10) {
            showToast('error', 'Açıklama en az 10 karakter olmalı.');
            return;
        }

        // Submit state
        submitBtn.disabled = true;
        spinner.hidden = false;
        label.textContent = 'Oluşturuluyor...';

        try {
            const response = await fetch('/Characters/Create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({
                    name: name,
                    prompt: prompt,
                    characterType: characterType,
                    assetId: currentAssetId
                })
            });

            if (!response.ok) {
                // Backend hata yanıtı: { success: false, message: "..." }
                let message = 'Karakter oluşturulamadı';
                try {
                    const error = await response.json();
                    if (error && error.message) message = error.message;
                } catch (_) { /* JSON parse edilemezse default mesaj */ }
                throw new Error(message);
            }

            // Başarılı: { success, message, data }
            await response.json();

            showToast('success', 'Karakter başarıyla oluşturuldu!');

            resetCreationForm();

            // F.6.3 — yeni karakter grid'e dahil olsun diye listeyi taze çek
            await loadCharacters();

            // Grid view'a dön
            switchToView('grid');

        } catch (err) {
            console.error('Karakter oluşturma hatası:', err);
            showToast('error', err.message);
        } finally {
            submitBtn.disabled = false;
            spinner.hidden = true;
            label.textContent = 'Oluştur';
        }
    }

    function resetCreationForm() {
        const form = document.getElementById('characterCreateForm');
        if (form) form.reset();

        const previewWrap = document.getElementById('characterUploadPreview');
        const emptyWrap = document.getElementById('characterUploadEmpty');
        const previewImg = document.getElementById('characterPreviewImg');
        const assetIdInput = document.getElementById('characterAssetIdInput');

        if (previewWrap) previewWrap.hidden = true;
        if (emptyWrap) emptyWrap.hidden = false;
        if (previewImg) previewImg.src = '';
        if (assetIdInput) assetIdInput.value = '';
        currentAssetId = null;
    }

    function getAntiForgeryToken() {
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenEl ? tokenEl.value : '';
    }

    function showToast(type, message) {
        if (typeof Toast !== 'undefined') {
            if (type === 'success' && Toast.success) { Toast.success(message); return; }
            if (type === 'error' && Toast.error) { Toast.error(message); return; }
            if (type === 'info' && Toast.info) { Toast.info(message); return; }
        }
        alert(message);
    }

    // Public API
    window.CharacterModal = {
        open: openModal,
        close: closeModal,
        refresh: loadCharacters   // F.6.3 — dışarıdan listeyi yenileme imkanı
    };
})();
