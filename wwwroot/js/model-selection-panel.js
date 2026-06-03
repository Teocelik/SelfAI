
/**
 * Model Selection Panel Module
 * Handles model selection, categories, and multi-select functionality
 */

const ModelSelectionPanel = (function () {
    'use strict';

    // DOM Elements
    let modelSelectBtn = null;
    let modelPanel = null;
    let modelArrow = null;
    let modelCategoryBtns = null;
    let modelContents = null;
    let modelOptions = null;
    let selectedModelValue = null;
    let selectedStyleValue = null;
    let selectedModelBaseModel = null;
    let selectedModelName = null;
    let selectedModelText = null;
    let selectedModelsContainer = null;
    let selectedModelsTags = null;
    let clearAllModelsBtn = null;

    // State
    let isPanelOpen = false;
    let selectedModels = [];

    /**
     * Initialize the model selection panel
     */
    function init() {
        cacheElements();
        bindEvents();
    }

    /**
     * Cache DOM elements
     */
    function cacheElements() {
        modelSelectBtn = document.getElementById('modelSelectBtn');
        modelPanel = document.getElementById('modelPanel');
        modelArrow = document.getElementById('modelArrow');
        modelCategoryBtns = document.querySelectorAll('.model-category-btn');
        modelContents = document.querySelectorAll('.model-content');
        modelOptions = document.querySelectorAll('.model-option');
        selectedModelValue = document.getElementById('selectedModelValue');
        selectedStyleValue = document.getElementById('selectedStyleValue');
        selectedModelBaseModel = document.getElementById('selectedModelBaseModel');
        selectedModelName = document.getElementById('selectedModelName');
        selectedModelText = document.getElementById('selectedModelText');
        selectedModelsContainer = document.getElementById('selectedModelsContainer');
        selectedModelsTags = document.getElementById('selectedModelsTags');
        clearAllModelsBtn = document.getElementById('clearAllModelsBtn');
    }

    /**
     * Bind event listeners
     */
    function bindEvents() {
        // Toggle button
        if (modelSelectBtn) {
            modelSelectBtn.addEventListener('click', handleToggleClick);
        }

        // Category buttons
        if (modelCategoryBtns) {
            modelCategoryBtns.forEach(btn => {
                btn.addEventListener('click', handleCategoryClick);
            });
        }

        // Model options - initial binding
        bindModelOptionEvents();

        // Clear all button
        if (clearAllModelsBtn) {
            clearAllModelsBtn.addEventListener('click', handleClearAll);
        }

        // Close on outside click
        document.addEventListener('click', handleOutsideClick);

        // Close on Escape key
        document.addEventListener('keydown', handleEscapeKey);
    }

    /**
     * ✅ YENİ: Model option eventlerini bağla
     */
    function bindModelOptionEvents() {
        modelOptions = document.querySelectorAll('.model-option');

        if (modelOptions) {
            modelOptions.forEach(option => {
                // Önceki listener'ı kaldır (duplicate önleme)
                option.removeEventListener('click', handleModelOptionClick);
                // Yeni listener ekle
                option.addEventListener('click', handleModelOptionClick);
            });
        }

        console.log('[ModelSelectionPanel] Bound events to', modelOptions?.length || 0, 'model options');
    }

    /**
     * ✅ YENİ: Dinamik içerik sonrası model option'ları yeniden bağla
     * Bu fonksiyon flux-models-loader.js'den çağrılacak
     */
    function refreshModelOptions() {
        console.log('[ModelSelectionPanel] Refreshing model options...');
        bindModelOptionEvents();
        updateOptionStates(); // Seçili olanları tekrar işaretle
    }

    /**
     * Handle toggle button click
     */
    function handleToggleClick(e) {
        e.preventDefault();
        if (isPanelOpen) {
            close();
        } else {
            open();
        }
    }

    /**
     * Open the panel
     */
    function open() {
        if (isPanelOpen || !modelPanel) return;
        isPanelOpen = true;

        modelPanel.classList.remove('hidden');
        if (modelArrow) modelArrow.style.transform = 'rotate(180deg)';

        setTimeout(() => {
            modelPanel.classList.add('open');
        }, 10);
    }

    /**
     * Close the panel
     */
    function close() {
        if (!isPanelOpen || !modelPanel) return;
        isPanelOpen = false;

        modelPanel.classList.remove('open');
        if (modelArrow) modelArrow.style.transform = 'rotate(0deg)';

        setTimeout(() => {
            modelPanel.classList.add('hidden');
        }, 300);
    }

    /**
     * Handle category button click
     */
    function handleCategoryClick() {
        const category = this.dataset.category;

        // Update active state
        if (modelCategoryBtns) {
            modelCategoryBtns.forEach(b => b.classList.remove('active'));
        }
        this.classList.add('active');

        // Show corresponding content
        if (modelContents) {
            modelContents.forEach(content => {
                content.classList.add('hidden');
            });
        }

        const targetContent = document.getElementById(`${category}-content`);
        if (targetContent) {
            targetContent.classList.remove('hidden');
        }
    }

    /**
     * Handle model option click
     */
    function handleModelOptionClick(e) {
        // this yerine e.currentTarget kullan (arrow function uyumluluğu için)
        const option = e.currentTarget;
        // Yeni kartlarda data-model/data-style var; eski hardcoded kartlarda yoksa fallback uygula.
        const model = option.dataset.model || option.dataset.baseModel || 'Flux';
        const style = option.dataset.style || option.dataset.name;
        const baseModel = option.dataset.baseModel || 'flux';
        const img = option.dataset.img;

        console.log('[ModelSelectionPanel] Card clicked:', { model, style, baseModel, img });

        addModel(model, style, baseModel, img);

        // Visual feedback
        const imgContainer = option.querySelector('div');
        if (imgContainer) {
            imgContainer.style.transform = 'scale(0.95)';
            setTimeout(() => {
                imgContainer.style.transform = 'scale(1)';
            }, 150);
        }
    }

    /**
     * Add (model, style) pair to selection
     */
    function addModel(model, style, baseModel, img) {
        // Aynı (model, style) çifti unique key — toggle davranışı
        const existingIndex = selectedModels.findIndex(m => m.model === model && m.style === style);

        if (existingIndex > -1) {
            // If already selected, remove it (toggle behavior)
            selectedModels.splice(existingIndex, 1);
        } else {
            // Add new pair
            selectedModels.push({
                model: model,
                style: style,
                baseModel: baseModel,
                img: img
            });
        }

        renderTags();
        updateOptionStates();
        updateHiddenInputs();
    }

    /**
     * Remove (model, style) pair from selection
     */
    function removeModel(model, style) {
        selectedModels = selectedModels.filter(m => !(m.model === model && m.style === style));
        renderTags();
        updateOptionStates();
        updateHiddenInputs();
    }

    /**
     * Render selected model tags
     */
    function renderTags() {
        if (!selectedModelsTags) return;

        selectedModelsTags.innerHTML = '';

        if (selectedModels.length === 0) {
            if (selectedModelsContainer) selectedModelsContainer.classList.add('hidden');
            if (selectedModelText) {
                selectedModelText.textContent = 'Select a model to start generating images';
                selectedModelText.classList.remove('text-primary');
                selectedModelText.classList.add('text-text-secondary');
            }
            return;
        }

        if (selectedModelsContainer) selectedModelsContainer.classList.remove('hidden');

        selectedModels.forEach(pair => {
            const tag = document.createElement('div');
            tag.className = 'selected-model-tag';

            // Etikette style adı gösterilir (Flux tek olduğu için kartlar style ile anılır)
            const shortName = pair.style.length > 10 ? pair.style.substring(0, 10) + '...' : pair.style;

            tag.innerHTML = `
                <img src="${pair.img}" alt="${pair.style}" />
                <span title="${pair.style}">${shortName}</span>
                <button type="button" class="remove-tag" data-model="${pair.model}" data-style="${pair.style}">
                    <i class="fas fa-times"></i>
                </button>
            `;
            selectedModelsTags.appendChild(tag);
        });

        // Update button text
        if (selectedModelText) {
            selectedModelText.textContent = `${selectedModels.length} model${selectedModels.length > 1 ? 's' : ''} selected`;
            selectedModelText.classList.remove('text-text-secondary');
            selectedModelText.classList.add('text-primary');
        }

        // Add click handlers for remove buttons (model + style çifti ile)
        document.querySelectorAll('.remove-tag').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                removeModel(btn.dataset.model, btn.dataset.style);
            });
        });
    }

    /**
  * Update visual state of model options
  */
    function updateOptionStates() {
        // Her zaman güncel elementleri al
        const currentModelOptions = document.querySelectorAll('.model-option');

        currentModelOptions.forEach(option => {
            // Çift bazlı eşleşme; eski hardcoded kartlar için fallback
            const model = option.dataset.model || option.dataset.baseModel || 'Flux';
            const style = option.dataset.style || option.dataset.name;
            // ✅ Basitçe ilk div'i seç (aspect-[3/4] olan div)
            const imgContainer = option.firstElementChild;

            if (selectedModels.find(m => m.model === model && m.style === style)) {
                option.classList.add('selected');
                if (imgContainer) {
                    imgContainer.classList.remove('border-transparent');
                    imgContainer.classList.add('border-primary');
                }
            } else {
                option.classList.remove('selected');
                if (imgContainer) {
                    imgContainer.classList.remove('border-primary');
                    imgContainer.classList.add('border-transparent');
                }
            }
        });
    }

    /**
     * Update hidden inputs for DTO binding
     */
    function updateHiddenInputs() {
        if (selectedModels.length > 0) {
            // Model ve Style senkron çiftler halinde yazılır:
            //   Model = "Flux,Flux"   Style = "Cinematic,Anime"
            if (selectedModelValue) selectedModelValue.value = selectedModels.map(m => m.model).join(',');
            if (selectedStyleValue) selectedStyleValue.value = selectedModels.map(m => m.style).join(',');

            // StyleDetail ilk çiftten doldurulur (Phase 1 DTO hizalaması korunur)
            const first = selectedModels[0];
            if (selectedModelBaseModel) selectedModelBaseModel.value = first.baseModel;
            if (selectedModelName) selectedModelName.value = first.style;

            console.log('[ModelSelectionPanel] Hidden inputs updated:', {
                model: selectedModelValue?.value,
                style: selectedStyleValue?.value
            });
        } else {
            if (selectedModelValue) selectedModelValue.value = '';
            if (selectedStyleValue) selectedStyleValue.value = '';
            if (selectedModelBaseModel) selectedModelBaseModel.value = '';
            if (selectedModelName) selectedModelName.value = '';
        }
    }

    /**
     * Handle clear all click
     */
    function handleClearAll(e) {
        e.stopPropagation();
        clearAll();
    }

    /**
     * Clear all selected models
     */
    function clearAll() {
        selectedModels = [];
        renderTags();
        updateOptionStates();
        updateHiddenInputs();
    }

    /**
     * Handle outside click
     */
    function handleOutsideClick(e) {
        if (isPanelOpen &&
            modelPanel &&
            modelSelectBtn &&
            !modelPanel.contains(e.target) &&
            !modelSelectBtn.contains(e.target) &&
            !e.target.closest('.selected-model-tag')) {
            close();
        }
    }

    /**
     * Handle Escape key
     */
    function handleEscapeKey(e) {
        if (e.key === 'Escape' && isPanelOpen) {
            close();
        }
    }

    /**
     * Check if panel is open
     * @returns {boolean}
     */
    function isOpen() {
        return isPanelOpen;
    }

    /**
     * Get selected models
     * @returns {Array}
     */
    function getSelectedModels() {
        return [...selectedModels];
    }

    /**
     * Reset the panel
     */
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
        getSelectedModels,
        clearAll,
        reset,
        refreshModelOptions  // : Dışarıdan erişilebilir
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModelSelectionPanel;
}