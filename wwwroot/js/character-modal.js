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
    let selectedFiles = [];   // F.M.4 — create formunda seçilen yüz görselleri (File[])

    // F.6.4a — Bekleyen archive işlemleri. characterId → { timeoutId, displayTimeoutId, card, character }
    // timeoutId: 5sn undo penceresi (dolunca backend'e archive). displayTimeoutId: 350ms fade-out
    // sonrası kartı layout'tan kaldırma. Undo ikisini de iptal eder; backend'e hiç istek gitmez.
    let pendingArchives = new Map();

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

            const json = await response.json();
            // ServiceResult sarmalı: { success, message, data: { items, totalCount } }.
            // Gerçek path tek seviye (data.items). Defansif: nested (data.items) + flat (items)
            // + PascalCase (Data/Items) varyasyonlarını da destekle.
            const payload = json.data || json.Data || json;
            const items = payload.items || payload.Items || [];

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

            // Hata fallback → empty list göster (kullanıcı yine de "Yeni Karakter" yapabilsin)
            if (emptyListState) emptyListState.hidden = false;
            gridContainer.hidden = true;
            cards = [];

            if (typeof Toast !== 'undefined' && Toast.error) {
                Toast.error('Karakterler yüklenemedi: ' + err.message);
            }
        } finally {
            // GARANTİ — loading state her durumda (success/error/erken çıkış) kapanır
            if (loadingState) loadingState.hidden = true;
        }
    }

    // F.6.5 — İki section: "KARAKTERLERİN" (kullanıcı) + "SİSTEM KARAKTERLERİ" (Affogato).
    // Boş section render edilmez. cards listesi iki section'daki kartların birleşimidir
    // (search filtresi tüm kartlar üzerinde çalışır).
    function renderCharacters(items) {
        if (!gridContainer) return;

        gridContainer.innerHTML = '';

        const selectedId = getCurrentSelectedCharacterId();

        const userChars = items.filter(function (c) { return !c.isSystemCharacter; });
        const systemChars = items.filter(function (c) { return c.isSystemCharacter; });

        // Boş section başlığı render edilmesin: createSection boş array'de null döner,
        // ayrıca burada da null kontrolü yapılır (çift güvence).
        const userSection = createSection('KARAKTERLERİN', userChars, selectedId);
        if (userSection) gridContainer.appendChild(userSection);

        const systemSection = createSection('SİSTEM KARAKTERLERİ', systemChars, selectedId);
        if (systemSection) gridContainer.appendChild(systemSection);

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

    function createSection(title, characters, currentSelectedId) {
        // Boş section render edilmesin (başlık tek başına görünmesin).
        if (!characters || characters.length === 0) {
            return null;
        }

        const section = document.createElement('div');
        section.className = 'character-modal__section';

        const heading = document.createElement('h3');
        heading.className = 'character-modal__section-title';
        heading.textContent = title;
        section.appendChild(heading);

        const grid = document.createElement('div');
        grid.className = 'character-modal__section-grid';

        characters.forEach(function (character) {
            grid.appendChild(createCharacterCard(character, currentSelectedId));
        });

        section.appendChild(grid);
        return section;
    }

    function createCharacterCard(character, currentSelectedId) {
        // F.M.4 — training durumu. "Ready" dışındaki kartlar seçilemez.
        const trainingStatus = character.trainingStatus || 'Ready';
        const isReady = trainingStatus === 'Ready';
        const isFailed = trainingStatus === 'Failed';

        const card = document.createElement('button');
        card.type = 'button';
        card.className = 'character-card';
        card.dataset.characterId = character.id;
        card.dataset.characterName = character.name;
        card.dataset.isSystem = character.isSystemCharacter ? 'true' : 'false';
        card.dataset.trainingStatus = trainingStatus;
        if (character.failureReason) card.dataset.failureReason = character.failureReason;

        if (!isReady) card.classList.add('is-not-ready');
        if (isFailed) card.classList.add('is-failed');

        if (currentSelectedId && character.id === currentSelectedId) {
            card.classList.add('is-selected');
        }

        // F.M.4 — durum rozeti (Ready dışında gösterilir)
        if (!isReady) {
            const badge = document.createElement('span');
            badge.className = 'character-card__status-badge';
            if (isFailed) {
                badge.classList.add('character-card__status-badge--failed');
                badge.innerHTML = '<i class="fas fa-exclamation-triangle"></i> Başarısız';
            } else {
                // Pending / Uploading / Training
                badge.classList.add('character-card__status-badge--training');
                badge.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Eğitiliyor...';
            }
            card.appendChild(badge);
        }

        // F.6.5 — Archive (sil) butonu YALNIZCA kullanıcı karakterlerinde. Sistem
        // karakterleri arşivlenemez (backend de 403 ile reddeder), bu yüzden butonu
        // hiç oluşturmuyoruz.
        if (!character.isSystemCharacter) {
            // F.6.4a — Hover'da fade-in olur, tıklanınca card click'i tetiklemeden
            // archive akışını başlatır.
            const archiveBtn = document.createElement('button');
            archiveBtn.type = 'button';
            archiveBtn.className = 'character-card__archive';
            archiveBtn.setAttribute('aria-label', 'Karakteri sil');
            archiveBtn.innerHTML = '<i class="fas fa-times"></i>';
            archiveBtn.addEventListener('click', function (e) {
                e.stopPropagation();   // Kart seçimini (handleCardClick) tetiklemesin
                handleArchiveRequest(card, character);
            });
            card.appendChild(archiveBtn);
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
        // F.M.4 — Ready olmayan karakter seçilemez.
        const trainingStatus = card.dataset.trainingStatus || 'Ready';
        if (trainingStatus !== 'Ready') {
            if (trainingStatus === 'Failed') {
                const reason = card.dataset.failureReason || 'Eğitim başarısız oldu.';
                if (typeof Toast !== 'undefined' && Toast.error) Toast.error(reason, 'Karakter Hazır Değil');
            } else if (typeof Toast !== 'undefined' && Toast.warning) {
                Toast.warning('Karakter henüz hazır değil, eğitim sürüyor.', 'Lütfen Bekle');
            }
            return;
        }

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
       F.6.4a — Karakter archive (sil): confirm + optimistic UI + 5sn undo penceresi
       Backend'e (POST /Characters/Archive) yalnızca undo penceresi dolunca istek gider.
       ──────────────────────────────────────────────────────────────────────── */

    function handleArchiveRequest(card, character) {
        // F.6.5 — Defansif: sistem karakterlerinde archive butonu hiç oluşturulmaz,
        // bu fonksiyon onlar için çağrılmamalı. Yine de güvenlik için erken çık.
        if (character.isSystemCharacter) {
            console.warn('Sistem karakteri arşivlenemez.');
            return;
        }

        // C2 — Aynı karaktere art arda archive: önceki bekleyen timeout'ları temizle,
        // yeni pencereyi baştan başlat (defansif; pratikte kart 350ms sonra gizlendiği için nadir).
        if (pendingArchives.has(character.id)) {
            const existing = pendingArchives.get(character.id);
            clearTimeout(existing.timeoutId);
            clearTimeout(existing.displayTimeoutId);
            pendingArchives.delete(character.id);
        }

        // Confirm dialog (native — görev gereği yeterli)
        const confirmed = confirm('"' + character.name + '" karakterini silmek istediğine emin misin?');
        if (!confirmed) return;

        // Silinen karakter şu an seçiliyse seçimi temizle (character-panel.js'in mevcut akışı)
        const selectedId = getCurrentSelectedCharacterId();
        if (selectedId && selectedId === character.id) {
            if (window.CharacterPanel && typeof window.CharacterPanel.clearCharacter === 'function') {
                window.CharacterPanel.clearCharacter();
            }
        }

        // Optimistic UI — kartı fade-out'a al
        card.classList.add('is-archiving');

        // 350ms (fade animasyonu) sonra layout'tan kaldır + cards listesinden çıkar
        const displayTimeoutId = setTimeout(function () {
            card.style.display = 'none';
            cards = cards.filter(function (c) { return c !== card; });

            // Son kart da gittiyse empty list state göster
            if (cards.length === 0) {
                if (gridContainer) gridContainer.hidden = true;
                if (emptyListState) emptyListState.hidden = false;
            }
        }, 350);

        // 5sn undo penceresi — dolunca backend'e gerçek archive isteği
        const timeoutId = setTimeout(function () {
            confirmArchive(character.id);
            pendingArchives.delete(character.id);
        }, 5000);

        pendingArchives.set(character.id, {
            timeoutId: timeoutId,
            displayTimeoutId: displayTimeoutId,
            card: card,
            character: character
        });

        showUndoToast(character);
    }

    function showUndoToast(character) {
        // Toast modülü undo butonu desteklemediği için özel, kalıcı bir undo toast'u kullanırız.
        let toastContainer = document.getElementById('characterUndoToast');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'characterUndoToast';
            toastContainer.className = 'character-undo-toast';
            document.body.appendChild(toastContainer);
        }

        toastContainer.innerHTML = '';

        const message = document.createElement('span');
        message.className = 'character-undo-toast__message';
        const icon = document.createElement('i');
        icon.className = 'fas fa-check-circle';
        message.appendChild(icon);
        message.appendChild(document.createTextNode(' "' + character.name + '" silindi'));

        const undoBtn = document.createElement('button');
        undoBtn.type = 'button';
        undoBtn.className = 'character-undo-toast__undo';
        undoBtn.textContent = 'Geri Al';
        undoBtn.addEventListener('click', function () {
            undoArchive(character.id);
        });

        const closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'character-undo-toast__close';
        closeBtn.setAttribute('aria-label', 'Kapat');
        closeBtn.innerHTML = '<i class="fas fa-times"></i>';
        closeBtn.addEventListener('click', function () {
            // Hemen onayla — undo penceresini atla, backend'e gönder
            const pending = pendingArchives.get(character.id);
            if (pending) {
                clearTimeout(pending.timeoutId);
                confirmArchive(character.id);
                pendingArchives.delete(character.id);
            }
            toastContainer.classList.remove('is-visible');
        });

        toastContainer.appendChild(message);
        toastContainer.appendChild(undoBtn);
        toastContainer.appendChild(closeBtn);

        // Görünür yap (transition tetiklensin diye next frame'de class ekle)
        requestAnimationFrame(function () {
            toastContainer.classList.add('is-visible');
        });

        // 5sn sonra otomatik gizle (undo penceresi ile senkron)
        setTimeout(function () {
            toastContainer.classList.remove('is-visible');
        }, 5000);
    }

    function undoArchive(characterId) {
        const pending = pendingArchives.get(characterId);
        if (!pending) return;

        // Backend isteği gönderilmeyecek — her iki timeout'u da iptal et
        clearTimeout(pending.timeoutId);
        clearTimeout(pending.displayTimeoutId);
        pendingArchives.delete(characterId);

        // Kartı geri göster
        pending.card.style.display = '';
        pending.card.classList.remove('is-archiving');

        // 350ms display-timeout undo'dan önce çalıştıysa kart cards'tan çıkmıştır; yoksa
        // hâlâ içindedir. Çift eklemeyi önlemek için yalnızca eksikse geri koy.
        if (cards.indexOf(pending.card) === -1) {
            cards.push(pending.card);
        }

        // Empty state göründüyse gizle, grid'i geri getir
        if (gridContainer) gridContainer.hidden = false;
        if (emptyListState) emptyListState.hidden = true;

        // Toast'u gizle
        const toastContainer = document.getElementById('characterUndoToast');
        if (toastContainer) toastContainer.classList.remove('is-visible');

        if (typeof Toast !== 'undefined' && Toast.info) {
            Toast.info('"' + pending.character.name + '" geri alındı.');
        }
    }

    async function confirmArchive(characterId) {
        try {
            const response = await fetch('/Characters/Archive', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({ characterId: characterId })
            });

            if (!response.ok) {
                // Backend hata yanıtı: { success: false, message: "..." }
                let serverMessage = 'Karakter silinemedi';
                try {
                    const error = await response.json();
                    if (error && error.message) serverMessage = error.message;
                } catch (_) { /* JSON parse edilemezse default mesaj */ }
                throw new Error(serverMessage);
            }

            // Başarılı — UI zaten kaldırılmış durumda, ekstra değişiklik yok (sessiz başarı)

        } catch (err) {
            console.error('Archive backend hatası:', err);

            if (typeof Toast !== 'undefined' && Toast.error) {
                Toast.error('Karakter sunucuda silinemedi: ' + err.message);
            }

            // Kartı geri getir — DB'den taze liste çek (kullanıcı tekrar deneyebilir)
            await loadCharacters();
        }
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

        // Bug 2 — create view'a geçerken submit butonunu temiz/initial state'e al
        // (önceki başarısız submit'ten kalan disabled/spinner/"Oluşturuluyor..." sızmasın).
        if (viewName === 'create') {
            const submitBtn = document.getElementById('characterCreateSubmit');
            if (submitBtn) {
                submitBtn.disabled = false;
                const spinner = submitBtn.querySelector('.character-create-form__submit-spinner');
                const label = submitBtn.querySelector('.character-create-form__submit-label');
                if (spinner) spinner.hidden = true;
                if (label) label.textContent = 'Oluştur';
            }
        }
    }

    /* ────────────────────────────────────────────────────────────────────────
       F.6.2 — Karakter oluşturma formu
       ──────────────────────────────────────────────────────────────────────── */

    // F.M.4 — çoklu yüz görseli yükleme (multipart). Asset upload backend'de yapılır,
    // frontend dosyaları doğrudan /Characters/Create'e gönderir.
    const MAX_FACE_FILES = 10;
    const MAX_FACE_SIZE = 15 * 1024 * 1024;  // 15MB

    function initCreationForm() {
        const form = document.getElementById('characterCreateForm');
        const uploadZone = document.getElementById('characterUploadZone');
        const fileInput = document.getElementById('characterFaceInput');

        if (!form) return;

        // Upload zone tıklaması → dosya seçici (thumbnail remove butonu hariç)
        uploadZone.addEventListener('click', function (e) {
            if (e.target.closest('.character-create-form__thumb-remove')) return;
            fileInput.click();
        });

        // Dosyalar seçildi → in-memory listeye ekle + thumbnail render
        fileInput.addEventListener('change', function (e) {
            addFaceFiles(Array.from(e.target.files || []));
            fileInput.value = '';  // aynı dosyayı tekrar seçebilmek için sıfırla
        });

        // Form submit
        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            await handleCreateSubmit();
        });
    }

    function addFaceFiles(files) {
        for (const file of files) {
            if (selectedFiles.length >= MAX_FACE_FILES) {
                showToast('warning', 'En fazla ' + MAX_FACE_FILES + ' görsel ekleyebilirsin.');
                break;
            }
            if (!file.type || !file.type.startsWith('image/')) {
                showToast('error', 'Sadece görsel dosyaları kabul edilir.');
                continue;
            }
            if (file.size > MAX_FACE_SIZE) {
                showToast('error', '"' + file.name + '" 15MB sınırını aşıyor.');
                continue;
            }
            selectedFiles.push(file);
        }
        renderFaceThumbs();
    }

    function renderFaceThumbs() {
        const grid = document.getElementById('characterUploadGrid');
        const emptyWrap = document.getElementById('characterUploadEmpty');
        const countEl = document.getElementById('characterUploadCount');
        if (!grid) return;

        grid.innerHTML = '';

        if (selectedFiles.length === 0) {
            grid.hidden = true;
            if (emptyWrap) emptyWrap.hidden = false;
            if (countEl) countEl.hidden = true;
            return;
        }

        if (emptyWrap) emptyWrap.hidden = true;
        grid.hidden = false;

        selectedFiles.forEach(function (file, index) {
            const thumb = document.createElement('div');
            thumb.className = 'character-create-form__thumb';

            const img = document.createElement('img');
            img.alt = file.name;
            const reader = new FileReader();
            reader.onload = function (ev) { img.src = ev.target.result; };
            reader.readAsDataURL(file);
            thumb.appendChild(img);

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'character-create-form__thumb-remove';
            removeBtn.setAttribute('aria-label', 'Görseli kaldır');
            removeBtn.innerHTML = '<i class="fas fa-times"></i>';
            removeBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                selectedFiles.splice(index, 1);
                renderFaceThumbs();
            });
            thumb.appendChild(removeBtn);

            grid.appendChild(thumb);
        });

        if (countEl) {
            countEl.hidden = false;
            countEl.textContent = selectedFiles.length + ' görsel seçildi'
                + (selectedFiles.length < 4 ? ' (4-6 önerilir)' : '');
        }
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
        if (selectedFiles.length < 1) {
            showToast('error', 'En az 1 yüz görseli yükle (4-6 önerilir).');
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
        label.textContent = 'Başlatılıyor...';

        try {
            // F.M.4 — multipart: dosyalar FaceImages[] olarak gider, Content-Type
            // header'ını ELLE set ETME (browser multipart boundary'i kendi ekler).
            const formData = new FormData();
            formData.append('Name', name);
            formData.append('Prompt', prompt);
            formData.append('CharacterType', characterType);
            selectedFiles.forEach(function (file) {
                formData.append('FaceImages', file);
            });

            const response = await fetch('/Characters/Create', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: formData
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

            // Başarı: { success, message, data: { characterId } }. Training arka planda başladı.
            const successJson = await response.json();
            const msg = successJson.message || 'Karakter eğitimi başlatıldı. Yaklaşık 5 dakika sürer.';
            showToast('success', msg);

            // F.M.UI.1b — Ekran altı progress bar. Modal AÇIK kalır (karar); bar
            // arka planda görünür, CharacterTrainingUpdate (SignalR) gelince app.js gizler.
            showTrainingProgress(name);

            resetCreationForm();

            // Yeni karakter "Eğitiliyor" badge'iyle grid'e dahil olsun diye listeyi taze çek
            await loadCharacters();

            // Grid view'a dön (sonuç SignalR "CharacterTrainingUpdate" ile gelecek)
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

        selectedFiles = [];

        const grid = document.getElementById('characterUploadGrid');
        const emptyWrap = document.getElementById('characterUploadEmpty');
        const countEl = document.getElementById('characterUploadCount');

        if (grid) { grid.innerHTML = ''; grid.hidden = true; }
        if (emptyWrap) emptyWrap.hidden = false;
        if (countEl) countEl.hidden = true;
    }

    function getAntiForgeryToken() {
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenEl ? tokenEl.value : '';
    }

    // F.M.UI.1b — Ekran altı training progress bar'ı göster. Gizleme app.js'te
    // CharacterTrainingUpdate (SignalR) handler'ında yapılır (Ready/Failed).
    function showTrainingProgress(characterName) {
        const progress = document.getElementById('trainingProgress');
        const title = document.getElementById('trainingProgressTitle');
        if (!progress) return;
        if (title) title.textContent = '"' + characterName + '" eğitiliyor';
        progress.hidden = false;
    }

    function showToast(type, message) {
        if (typeof Toast !== 'undefined') {
            if (type === 'success' && Toast.success) { Toast.success(message); return; }
            if (type === 'error' && Toast.error) { Toast.error(message); return; }
            if (type === 'warning' && Toast.warning) { Toast.warning(message); return; }
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
