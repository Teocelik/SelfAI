/**
 * Character Panel Module
 * Karakter seçim panelini yönetir: panel aç/kapat, mod seçimi, karakter seçimi,
 * Face Lock ile karşılıklı dışlama (mutual exclusivity) ve prompt @Name yönetimi.
 *
 * Face Lock paneliyle aynı görsel dili ve aynı .open/.hidden geçiş mantığını kullanır.
 */

const CharacterPanel = (function () {
    'use strict';

    // ─── DOM Referansları ───
    let characterBtn = null;
    let characterPanel = null;
    let closeBtn = null;
    let modeButtonsContainer = null;
    let modeDescription = null;
    let grid = null;
    let activeDot = null;

    // ─── Modül State ───
    let characters = [];                 // API'den gelen karakter listesi
    let selectedCharacter = null;        // { id, name } veya null
    let selectedMode = 'balanced';       // "flexible" | "balanced" | "strong"

    // Karakter görseli henüz API'den dönmüyor; Face Lock'taki gibi placeholder kullan
    const PLACEHOLDER_AVATAR =
        'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=120&h=120&fit=crop&crop=face';

    const MODE_DESCRIPTIONS = {
        flexible: 'Ideal for stylized characters and image generations.',
        balanced: 'Ideal for creating realistic images and more.',
        strong: 'Character similarity is most important.'
    };

    /**
     * Modülü başlat
     */
    function init() {
        cacheElements();
        bindEvents();
        // F.5c: Karakter listesi artık full-screen modal'daki statik (mock) Razor
        // kartlarından geliyor. Gerçek API entegrasyonu (loadCharactersFromBackend →
        // /Characters/List) F.6'da yeniden bağlanacak. Bu task'ta çağrı YOK:
        // aksi halde renderGrid() modal'daki mock kartları silerdi (ikisi de #characterGrid).
        // loadCharactersFromBackend();
        console.log('[CharacterPanel] initialized');
    }

    /**
     * DOM referanslarını al
     */
    function cacheElements() {
        characterBtn = document.getElementById('characterBtn');
        characterPanel = document.getElementById('characterPanel');
        closeBtn = document.getElementById('closeCharacterPanel');
        modeButtonsContainer = document.getElementById('characterModeButtons');
        modeDescription = document.getElementById('characterModeDescription');
        grid = document.getElementById('characterGrid');
        activeDot = document.getElementById('characterActiveDot');
    }

    /**
     * Event listener'ları bağla
     */
    function bindEvents() {
        // F.5c: Eski sidebar paneli kaldırıldı. Aç/kapat, dışına tıklama ve ESC
        // mantığı artık full-screen modal'da (character-modal.js) yaşıyor.
        // Karakter seçimi ve mode değişimi character-modal.js'ten public API ile
        // (CharacterPanel.selectCharacter / CharacterPanel.setMode) tetiklenir,
        // bu yüzden burada bağlanacak sidebar event'i kalmadı.
    }

    // ═══════════════════════════════════════════════
    // KARAKTER LİSTESİ
    // ═══════════════════════════════════════════════

    /**
     * Backend'den karakterleri yükle (GET /Characters/List)
     */
    async function loadCharactersFromBackend() {
        const response = await apiFetch('/Characters/List?page=1&pageSize=50', {
            method: 'GET'
        });

        // apiFetch hata durumunda null döner ve kendi toast'ını gösterir
        if (!response || !Array.isArray(response.data)) {
            console.error('[CharacterPanel] Karakter listesi yüklenemedi.');
            if (grid) {
                grid.innerHTML =
                    '<div class="text-gray-400 text-xs col-span-2 text-center py-4">Karakter listesi yüklenemedi</div>';
            }
            return;
        }

        characters = response.data;
        renderGrid();
    }

    /**
     * Karakter grid'ini çiz
     */
    function renderGrid() {
        if (!grid) return;
        grid.innerHTML = '';

        if (!characters.length) {
            grid.innerHTML =
                '<div class="text-gray-400 text-xs col-span-2 text-center py-4">Karakter bulunamadı</div>';
            return;
        }

        characters.forEach((character) => {
            grid.appendChild(createCharacterCard(character));
        });

        // Halihazırda seçili karakter varsa görsel state'i tazele
        if (selectedCharacter) {
            highlightSelectedCard();
        }
    }

    /**
     * Tek bir karakter kartı oluştur
     */
    function createCharacterCard(character) {
        const card = document.createElement('div');
        card.className =
            'character-card relative cursor-pointer bg-[#2A2A2A] hover:bg-[#333] border-2 border-transparent rounded-lg p-2 transition-all duration-200';
        card.dataset.characterId = character.id;
        card.dataset.characterName = character.name;

        // "Sample" rozeti (sistem karakterleri için)
        if (character.system_character === true) {
            const badge = document.createElement('span');
            badge.className =
                'absolute top-1 right-1 z-10 bg-gray-700 text-white text-[10px] px-1.5 py-0.5 rounded';
            badge.textContent = 'Sample';
            card.appendChild(badge);
        }

        // Avatar
        const avatar = document.createElement('div');
        avatar.className = 'w-full aspect-square rounded-md overflow-hidden border border-[#444]';
        const img = document.createElement('img');
        img.src = PLACEHOLDER_AVATAR;
        img.alt = character.name || 'Character';
        img.className = 'w-full h-full object-cover';
        avatar.appendChild(img);
        card.appendChild(avatar);

        // İsim etiketi
        const label = document.createElement('p');
        label.className = 'text-xs text-white text-center mt-1 truncate';
        label.textContent = character.name || '';
        card.appendChild(label);

        card.addEventListener('click', () => handleCharacterClick(card));
        return card;
    }

    // ═══════════════════════════════════════════════
    // KARAKTER SEÇİMİ
    // ═══════════════════════════════════════════════

    /**
     * Kart tıklaması — toggle davranışı
     */
    function handleCharacterClick(card) {
        const characterId = card.dataset.characterId;
        const characterName = card.dataset.characterName;

        if (selectedCharacter && selectedCharacter.id === characterId) {
            deselectCharacter();
        } else {
            selectCharacter({ id: characterId, name: characterName });
        }
    }

    /**
     * Bir karakteri seç
     */
    function selectCharacter({ id, name }) {
        // F.M.4 — Karakter sistemi fal.ai LoRA ile aktif. Seçim hidden input'lara
        // wire edilir, generate payload'una characterId + characterMode olarak gider.
        selectedCharacter = { id, name };

        // Hidden input'ları doldur
        setHiddenValue('characterId', id);
        setHiddenValue('characterMode', selectedMode);

        // ─── Mutex: Character aktif → Face Lock + Pose Lock otomatik temizlenir (F.M.6) ───
        // F.M.4'teki inline Face-disable mantığının yerini FeatureMutex aldı; artık üç
        // feature (Character/Face/Pose) tek noktadan karşılıklı dışlanır.
        if (window.FeatureMutex) window.FeatureMutex.setActive('character');

        // F.M.6 hotfix: Generate button state için değişimi bildir
        document.dispatchEvent(new CustomEvent('character-selection-changed', {
            detail: { characterId: id }
        }));

        // Not: Aktiflik göstergesi olarak butondaki avatar kullanılıyor (Face Lock stili),
        // bu yüzden ayrıca turkuaz dot gösterilmiyor.
        if (activeDot) activeDot.classList.add('hidden');

        // Kart vurgusu
        highlightSelectedCard();

        // NOT (F.M.4): Eskiden prompt'a @Name eklenirdi (Affogato). fal.ai LoRA'da
        // trigger word'ü backend prepend ettiği için prompt temiz bırakılır.

        // Butonda icon yerine seçilen karakterin kart görselini thumbnail olarak göster (Face Lock stili)
        const cardImg = document.querySelector(`[data-character-id="${id}"] img`)?.src;
        if (cardImg) updateCharacterBtnThumbnail(cardImg);

        console.log('[CharacterPanel] Karakter seçildi: %s (id: %s)', name, id);
    }

    /**
     * Seçimi kaldır
     */
    function deselectCharacter() {
        selectedCharacter = null;

        // Hidden input'ları sıfırla
        setHiddenValue('characterId', '');

        // Aktif göstergesini gizle
        if (activeDot) activeDot.classList.add('hidden');

        // Kart vurgusunu temizle
        clearCardHighlights();

        // (F.M.4: prompt'a @Name eklenmediği için temizleme de yapılmaz)

        // Mode default'a dön
        selectedMode = 'balanced';
        setHiddenValue('characterMode', 'balanced');
        applyModeStyles('balanced');

        // Butonda thumbnail'ı kaldır, icon'u geri getir
        updateCharacterBtnThumbnail(null);

        // F.M.6 hotfix: Generate button state için değişimi bildir
        document.dispatchEvent(new CustomEvent('character-selection-changed', {
            detail: { characterId: null }
        }));

        console.log('[CharacterPanel] Karakter seçimi kaldırıldı.');
    }

    /**
     * Buton thumbnail'ını Face Lock stili güncelle (face-lock-panel.js::updateButtonThumbnail birebir).
     * imageSrc verilirse: ikon gizlenir, butona sabit boyutlu thumbnail eklenir.
     * imageSrc null verilirse: thumbnail kaldırılır, ikon geri gelir.
     */
    function updateCharacterBtnThumbnail(imageSrc) {
        if (!characterBtn) return;

        const buttonContent = characterBtn.querySelector('.flex.flex-col');
        if (!buttonContent) return;

        const icon = buttonContent.querySelector('i.fa-user');
        let thumbnail = buttonContent.querySelector('.character-btn-thumbnail');

        if (imageSrc) {
            // Icon'u gizle
            if (icon) {
                icon.style.display = 'none';
            }

            // Thumbnail yoksa oluştur
            if (!thumbnail) {
                thumbnail = document.createElement('div');
                thumbnail.className = 'character-btn-thumbnail';
                thumbnail.innerHTML = `
                    <img src="" alt="Character" class="w-10 h-10 object-cover rounded-lg border-2 border-primary/50">
                `;
                // Icon'un yerine ekle (ilk child olarak)
                buttonContent.insertBefore(thumbnail, buttonContent.firstChild);
            }

            // Thumbnail'ı güncelle
            const thumbnailImg = thumbnail.querySelector('img');
            if (thumbnailImg) {
                thumbnailImg.src = imageSrc;
            }
        } else {
            // Thumbnail'ı kaldır ve icon'u göster
            if (thumbnail) {
                thumbnail.remove();
            }
            if (icon) {
                icon.style.display = '';
            }
        }
    }

    /**
     * Seçili kartı turkuaz border ile vurgula
     */
    function highlightSelectedCard() {
        if (!grid || !selectedCharacter) return;
        clearCardHighlights();
        const card = grid.querySelector(
            `.character-card[data-character-id="${cssEscape(selectedCharacter.id)}"]`
        );
        if (card) {
            card.classList.remove('border-transparent');
            card.classList.add('border-[#00CED1]');
        }
    }

    /**
     * Tüm kartlardan vurguyu kaldır
     */
    function clearCardHighlights() {
        if (!grid) return;
        grid.querySelectorAll('.character-card').forEach((card) => {
            card.classList.remove('border-[#00CED1]');
            card.classList.add('border-transparent');
        });
    }

    // ═══════════════════════════════════════════════
    // PROMPT @Name YÖNETİMİ
    // ═══════════════════════════════════════════════

    /**
     * Prompt'a @Name ekle.
     * Eğer prompt zaten bir önceki karakterin @Name'i ile başlıyorsa onu değiştir.
     */
    function insertMentionInPrompt(name, previousName) {
        const promptInput = document.getElementById('promptInput');
        if (!promptInput) return;

        const mention = `@${name} `;
        let value = promptInput.value;

        // Önceki seçimin mention'ı baştaysa onu kaldır
        if (previousName) {
            const prevMention = `@${previousName} `;
            if (value.startsWith(prevMention)) {
                value = value.substring(prevMention.length);
            }
        }

        promptInput.value = mention + value;
        promptInput.dispatchEvent(new Event('input', { bubbles: true }));
    }

    /**
     * Prompt'tan yalnızca kendi eklediğimiz @Name'i çıkar
     */
    function removeMentionFromPrompt(name) {
        const promptInput = document.getElementById('promptInput');
        if (!promptInput) return;

        const mention = `@${name} `;
        if (promptInput.value.startsWith(mention)) {
            promptInput.value = promptInput.value.substring(mention.length);
            promptInput.dispatchEvent(new Event('input', { bubbles: true }));
        }
    }

    /**
     * PUBLIC: Form submit öncesi prompt'taki @Name'leri {Name}'e çevir (geçmiş provider formatı).
     * Karakter seçili değilse girişi olduğu gibi döndürür.
     */
    function transformPromptForSubmit(prompt) {
        if (!selectedCharacter || typeof prompt !== 'string') return prompt;

        const mention = `@${selectedCharacter.name}`;
        const replacement = `{${selectedCharacter.name}}`;
        return prompt.replace(new RegExp(escapeRegex(mention), 'g'), replacement);
    }

    // ═══════════════════════════════════════════════
    // MOD SEÇİMİ
    // ═══════════════════════════════════════════════

    /**
     * PUBLIC: Mode'u ayarla — full-screen modal'daki mode toggle (character-modal.js)
     * bunu çağırır. Mevcut mode set mantığını kullanır (yeniden yazım yok).
     * @param {('flexible'|'balanced'|'strong')} mode
     */
    function setMode(mode) {
        if (!mode) return;
        selectedMode = mode;
        setHiddenValue('characterMode', mode);
        applyModeStyles(mode);
    }

    /**
     * Mode butonlarının görsel state'ini ve açıklamasını güncelle
     */
    function applyModeStyles(mode) {
        document.querySelectorAll('.character-mode-btn').forEach((btn) => {
            const isActive = btn.dataset.mode === mode;
            btn.classList.toggle('bg-[#00CED1]', isActive);
            btn.classList.toggle('text-white', isActive);
            btn.classList.toggle('bg-[#2A2A2A]', !isActive);
            btn.classList.toggle('text-gray-400', !isActive);
        });

        if (modeDescription && MODE_DESCRIPTIONS[mode]) {
            modeDescription.textContent = MODE_DESCRIPTIONS[mode];
        }
    }

    // ═══════════════════════════════════════════════
    // YARDIMCILAR
    // ═══════════════════════════════════════════════

    function setHiddenValue(id, value) {
        const el = document.getElementById(id);
        if (el) el.value = value;
    }

    /**
     * RegExp özel karakterlerini escape et
     */
    function escapeRegex(str) {
        return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    /**
     * CSS attribute selector için değer escape (CSS.escape fallback)
     */
    function cssEscape(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') {
            return window.CSS.escape(value);
        }
        return String(value).replace(/["\\]/g, '\\$&');
    }

    /**
     * PUBLIC: Karakter aktif mi? (mutex / disable kontrolü için)
     */
    function isCharacterActive() {
        return selectedCharacter !== null;
    }

    /**
     * PUBLIC: Seçili karakter ID'si (F.M.6 — generate button state + FeatureMutex için)
     * @returns {string|null}
     */
    function getSelectedCharacterId() {
        return selectedCharacter ? selectedCharacter.id : null;
    }

    // Public API
    return {
        init,
        transformPromptForSubmit,
        isCharacterActive,
        getSelectedCharacterId,   // F.M.6 hotfix — generate button state
        // F.5c: full-screen modal (character-modal.js) bu API'yi kullanır.
        // selectCharacter, characterData = { id, name, imageUrl } kabul eder;
        // imageUrl yok sayılır (thumbnail mevcut mantıkta kart DOM'undan türetilir).
        selectCharacter,
        setMode,
        // Re-click to deselect: modal'daki seçili karta tekrar tıklayınca çağrılır.
        // Mevcut deselectCharacter akışını (hidden input sıfırla, Face Lock'u geri aç,
        // prompt'tan @Name çıkar, mode reset, thumbnail kaldır) yeniden yazmadan çağırır.
        clearCharacter: deselectCharacter
    };
})();

// F.5c: character-modal.js modüle window.CharacterPanel üzerinden erişir
// (top-level const window'a otomatik bağlanmaz).
if (typeof window !== 'undefined') {
    window.CharacterPanel = CharacterPanel;
}

// Modül sistemleri için export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CharacterPanel;
}
