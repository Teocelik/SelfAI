/**
 * Generate Button State Module
 *
 * Generate butonunu prompt + seçim durumuna göre canlı olarak enable/disable eder.
 *   - Prompt boş → DISABLED
 *   - Prompt dolu VE (model seçili VEYA karakter seçili VEYA Face Lock aktif) → ENABLED
 *
 * F.M.6 hotfix: Face Lock veya Character aktifken backend endpoint'i override eder,
 * bu yüzden model seçimi ZORUNLU DEĞİL. Model sinyali #selectedModelValue hidden
 * input'undan; karakter/face sinyali ilgili panel public API'lerinden okunur.
 *
 * ⚠️ Generation sırasında buton durumunu ImageControls.setGenerateButtonState yönetir
 *    (loading/spinner). Çakışmayı önlemek için "busy" kilidi vardır: busy iken bu
 *    modülün validity güncellemeleri devre dışı kalır, ImageControls kazanır.
 */

const GenerateButtonState = (function () {
    'use strict';

    // DOM Elements
    let generateBtn = null;
    let promptInput = null;
    let selectedModelInput = null;

    // Generation sürerken validity güncellemelerini bastıran kilit
    let isBusy = false;

    /**
     * Initialize — App.init tarafından (DOMContentLoaded sonrası) çağrılır
     */
    function init() {
        cacheElements();

        if (!generateBtn || !promptInput) {
            console.warn('[GenerateButtonState] Gerekli element bulunamadı (generateBtn/promptInput)');
            return;
        }

        // İlk state hesabı (markup'ta default disabled, JS doğrular)
        updateState();

        // Prompt yazıldıkça canlı güncelle
        promptInput.addEventListener('input', updateState);

        // Seçim değişimlerinde güncelle
        document.addEventListener('model-selection-changed', updateState);       // F.M.5 model-picker
        document.addEventListener('character-selection-changed', updateState);   // F.M.6 character-panel
        document.addEventListener('face-lock-changed', updateState);             // F.M.6 face-lock-panel
        document.addEventListener('feature-mutex-changed', updateState);         // F.M.6 feature-mutex
    }

    /**
     * Cache DOM elements
     */
    function cacheElements() {
        generateBtn = document.getElementById('generateBtn');
        promptInput = document.getElementById('promptInput');
        selectedModelInput = document.getElementById('selectedModelValue');
    }

    /**
     * Buton durumunu prompt + model seçimine göre yeniden hesapla.
     * Busy (generation sürüyor) iken hiçbir şey yapmaz — ImageControls yönetir.
     */
    function updateState() {
        if (!generateBtn || !promptInput) return;
        if (isBusy) return;

        const hasPrompt = promptInput.value.trim().length > 0;
        // Model VEYA karakter VEYA Face Lock — herhangi biri yeterli (F.M.6 hotfix).
        const hasSelection = checkModelSelected() || checkCharacterSelected() || checkFaceLockActive();
        const shouldEnable = hasPrompt && hasSelection;

        generateBtn.disabled = !shouldEnable;
        generateBtn.setAttribute('aria-disabled', String(!shouldEnable));
    }

    /**
     * En az bir model seçili mi? Hidden input dolu ise evet.
     * @returns {boolean}
     */
    function checkModelSelected() {
        return !!(selectedModelInput && selectedModelInput.value.trim().length > 0);
    }

    /**
     * Karakter seçili mi? (F.M.6 — Character aktifken model gerekmez)
     * @returns {boolean}
     */
    function checkCharacterSelected() {
        return !!(window.CharacterPanel
            && typeof window.CharacterPanel.getSelectedCharacterId === 'function'
            && window.CharacterPanel.getSelectedCharacterId());
    }

    /**
     * Face Lock aktif mi? (F.M.6 — Face Lock aktifken model gerekmez)
     * @returns {boolean}
     */
    function checkFaceLockActive() {
        return !!(window.FaceLockPanel
            && typeof window.FaceLockPanel.getFaceAssetId === 'function'
            && window.FaceLockPanel.getFaceAssetId());
    }

    /**
     * Generation loading kilidini aç/kapat.
     * ImageControls.setGenerateButtonState bunu çağırır:
     *   - busy=true  → loading başladı, validity güncellemeleri pasif
     *   - busy=false → loading bitti (çağıran ayrıca updateState() ile durumu tazeler)
     * @param {boolean} busy
     */
    function setBusy(busy) {
        isBusy = !!busy;
    }

    // Public API
    return {
        init,
        updateState,
        setBusy
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = GenerateButtonState;
}
