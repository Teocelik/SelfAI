/**
 * Model Selection Panel Module (F.M.3 — tek-seçim, fal.ai endpoint tabanlı)
 *
 * Eski çoklu (model+style) seçim mantığı, fal.ai migration'ında tek bir model
 * endpoint seçimine sadeleştirildi. Kullanıcı bir model kartı seçer; kartın
 * data-model-endpoint değeri #selectedModelValue hidden input'una yazılır ve
 * generate isteğinin payload'undaki modelEndpoint'i olur.
 *
 * F.M.5'te dinamik catalog (IFalAiModelCatalog) kartları üretecek; bu modül
 * data-model-endpoint taşıyan her .model-option ile çalışmaya devam eder.
 */

const ModelSelectionPanel = (function () {
    'use strict';

    // DOM Elements
    let modelSelectBtn = null;
    let modelPanel = null;
    let modelArrow = null;
    let modelCategoryBtns = null;
    let modelContents = null;
    let selectedModelValue = null;   // hidden input (name="Model") — endpoint string buraya yazılır
    let selectedModelText = null;    // buton açıklama metni

    // State
    let isPanelOpen = false;
    let selectedEndpoint = null;     // örn. "fal-ai/flux/schnell" veya null

    function init() {
        cacheElements();
        bindEvents();
    }

    function cacheElements() {
        modelSelectBtn = document.getElementById('modelSelectBtn');
        modelPanel = document.getElementById('modelPanel');
        modelArrow = document.getElementById('modelArrow');
        modelCategoryBtns = document.querySelectorAll('.model-category-btn');
        modelContents = document.querySelectorAll('.model-content');
        selectedModelValue = document.getElementById('selectedModelValue');
        selectedModelText = document.getElementById('selectedModelText');
    }

    function bindEvents() {
        if (modelSelectBtn) {
            modelSelectBtn.addEventListener('click', handleToggleClick);
        }

        if (modelCategoryBtns) {
            modelCategoryBtns.forEach(btn => btn.addEventListener('click', handleCategoryClick));
        }

        bindModelOptionEvents();

        document.addEventListener('click', handleOutsideClick);
        document.addEventListener('keydown', handleEscapeKey);
    }

    /**
     * Yalnızca data-model-endpoint taşıyan kartlara click bağla.
     * (Legacy google/magic/pro kartları endpoint taşımadığı için yok sayılır.)
     */
    function bindModelOptionEvents() {
        const options = document.querySelectorAll('.model-option[data-model-endpoint]');
        options.forEach(option => {
            option.removeEventListener('click', handleModelOptionClick);
            option.addEventListener('click', handleModelOptionClick);
        });
        console.log('[ModelSelectionPanel] Bound events to', options.length, 'endpoint kartı');
    }

    /**
     * Dinamik içerik sonrası kartları yeniden bağla (F.M.5 catalog için API korunur).
     */
    function refreshModelOptions() {
        bindModelOptionEvents();
        updateOptionStates();
    }

    function handleToggleClick(e) {
        e.preventDefault();
        if (isPanelOpen) close(); else open();
    }

    function open() {
        if (isPanelOpen || !modelPanel) return;
        isPanelOpen = true;
        modelPanel.classList.remove('hidden');
        if (modelArrow) modelArrow.style.transform = 'rotate(180deg)';
        setTimeout(() => modelPanel.classList.add('open'), 10);
    }

    function close() {
        if (!isPanelOpen || !modelPanel) return;
        isPanelOpen = false;
        modelPanel.classList.remove('open');
        if (modelArrow) modelArrow.style.transform = 'rotate(0deg)';
        setTimeout(() => modelPanel.classList.add('hidden'), 300);
    }

    function handleCategoryClick() {
        const category = this.dataset.category;

        if (modelCategoryBtns) modelCategoryBtns.forEach(b => b.classList.remove('active'));
        this.classList.add('active');

        if (modelContents) modelContents.forEach(c => c.classList.add('hidden'));

        const target = document.getElementById(`${category}-content`);
        if (target) target.classList.remove('hidden');
    }

    /**
     * Model kartı tıklaması — tek seçim, aynı karta tekrar tıklamak seçimi kaldırır.
     */
    function handleModelOptionClick(e) {
        const option = e.currentTarget;
        const endpoint = option.dataset.modelEndpoint;
        if (!endpoint) return;

        if (selectedEndpoint === endpoint) {
            clearAll();
        } else {
            selectedEndpoint = endpoint;
            updateHiddenInputs();
            updateOptionStates();
            updateButtonText(option);
        }

        // Görsel feedback (tıklama animasyonu)
        const imgContainer = option.firstElementChild;
        if (imgContainer) {
            imgContainer.style.transform = 'scale(0.96)';
            setTimeout(() => { imgContainer.style.transform = 'scale(1)'; }, 150);
        }
    }

    /**
     * Seçili karta turkuaz border ver, diğerlerini temizle.
     */
    function updateOptionStates() {
        document.querySelectorAll('.model-option[data-model-endpoint]').forEach(option => {
            const imgContainer = option.firstElementChild;
            const isSelected = option.dataset.modelEndpoint === selectedEndpoint;

            option.classList.toggle('selected', isSelected);
            if (imgContainer) {
                imgContainer.classList.toggle('border-primary', isSelected);
                imgContainer.classList.toggle('border-transparent', !isSelected);
            }
        });
    }

    /**
     * Buton açıklamasını seçilen modelin adı + kredi ile güncelle.
     */
    function updateButtonText(option) {
        if (!selectedModelText) return;
        const label = option.dataset.modelLabel || 'Model';
        const cost = option.dataset.creditCost;
        selectedModelText.textContent = cost ? `${label} — ${cost} kredi` : label;
        selectedModelText.classList.remove('text-text-secondary');
        selectedModelText.classList.add('text-primary');
    }

    /**
     * Hidden input'u (endpoint) güncelle ve seçim değişimini yayınla
     * (generate-button-state.js bunu dinler).
     */
    function updateHiddenInputs() {
        if (selectedModelValue) selectedModelValue.value = selectedEndpoint || '';

        document.dispatchEvent(new CustomEvent('model-selection-changed', {
            detail: { selectedEndpoint: selectedEndpoint }
        }));
    }

    /**
     * Seçimi tamamen kaldır.
     */
    function clearAll() {
        selectedEndpoint = null;
        updateHiddenInputs();
        updateOptionStates();

        if (selectedModelText) {
            selectedModelText.textContent = 'Select a model to start generating images';
            selectedModelText.classList.remove('text-primary');
            selectedModelText.classList.add('text-text-secondary');
        }
    }

    function handleOutsideClick(e) {
        if (isPanelOpen && modelPanel && modelSelectBtn &&
            !modelPanel.contains(e.target) && !modelSelectBtn.contains(e.target)) {
            close();
        }
    }

    function handleEscapeKey(e) {
        if (e.key === 'Escape' && isPanelOpen) close();
    }

    function isOpen() {
        return isPanelOpen;
    }

    function getSelectedEndpoint() {
        return selectedEndpoint;
    }

    function reset() {
        clearAll();
        close();
    }

    // Public API
    return {
        init,
        open,
        close,
        isOpen,
        getSelectedEndpoint,
        clearAll,
        reset,
        refreshModelOptions
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModelSelectionPanel;
}
