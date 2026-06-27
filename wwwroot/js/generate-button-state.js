/**
 * Generate Button State Module
 *
 * Generate butonunu prompt + model seçimine göre canlı olarak enable/disable eder.
 *   - Prompt boş VEYA hiç model seçilmemiş  → DISABLED
 *   - Prompt dolu VE en az 1 model seçili    → ENABLED
 *
 * "Model seçili mi" sinyali olarak #selectedModelValue hidden input'u kullanılır
 * (ModelSelectionPanel bunu doldurur; backend'e giden Model değerinin ta kendisi).
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

        // Model seçimi değişince güncelle (model-selection-panel.js emit eder)
        document.addEventListener('model-selection-changed', updateState);
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
        const hasModel = checkModelSelected();
        const shouldEnable = hasPrompt && hasModel;

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
