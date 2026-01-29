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

        // Model options
        if (modelOptions) {
            modelOptions.forEach(option => {
                option.addEventListener('click', handleModelOptionClick);
            });
        }

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
    function handleModelOptionClick() {
        const baseModel = this.dataset.baseModel || this.dataset.model;
        const modelName = this.dataset.name;
        const modelImg = this.dataset.img;

        addModel(baseModel, modelName, modelImg);

        // Visual feedback
        const imgContainer = this.querySelector('div');
        if (imgContainer) {
            imgContainer.style.transform = 'scale(0.95)';
            setTimeout(() => {
                imgContainer.style.transform = 'scale(1)';
            }, 150);
        }
    }

    /**
     * Add model to selection
     */
    function addModel(baseModel, modelName, modelImg) {
        // Check if already selected
        const existingIndex = selectedModels.findIndex(m => m.name === modelName);

        if (existingIndex > -1) {
            // If already selected, remove it (toggle behavior)
            selectedModels.splice(existingIndex, 1);
        } else {
            // Add new model
            selectedModels.push({
                baseModel: baseModel,
                name: modelName,
                img: modelImg
            });
        }

        renderTags();
        updateOptionStates();
        updateHiddenInputs();
    }

    /**
     * Remove model from selection
     */
    function removeModel(modelName) {
        selectedModels = selectedModels.filter(m => m.name !== modelName);
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
            }
            if (selectedModelValue) selectedModelValue.value = '';
            return;
        }

        if (selectedModelsContainer) selectedModelsContainer.classList.remove('hidden');

        selectedModels.forEach(model => {
            const tag = document.createElement('div');
            tag.className = 'selected-model-tag';

            // Shorten long names
            const shortName = model.name.length > 10 ? model.name.substring(0, 10) + '...' : model.name;

            tag.innerHTML = `
                <img src="${model.img}" alt="${model.name}" />
                <span title="${model.name}">${shortName}</span>
                <button type="button" class="remove-tag" data-model="${model.name}">
                    <i class="fas fa-times"></i>
                </button>
            `;
            selectedModelsTags.appendChild(tag);
        });

        // Update hidden input with all selected model IDs
        if (selectedModelValue) {
            selectedModelValue.value = selectedModels.map(m => m.name).join(',');
        }

        // Update button text
        if (selectedModelText) {
            selectedModelText.textContent = `${selectedModels.length} model${selectedModels.length > 1 ? 's' : ''} selected`;
        }

        // Add click handlers for remove buttons
        document.querySelectorAll('.remove-tag').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const modelId = btn.dataset.model;
                removeModel(modelId);
            });
        });
    }

    /**
     * Update visual state of model options
     */
    function updateOptionStates() {
        if (!modelOptions) return;

        modelOptions.forEach(option => {
            const modelName = option.dataset.name;
            if (selectedModels.find(m => m.name === modelName)) {
                option.classList.add('selected');
            } else {
                option.classList.remove('selected');
            }
        });
    }

    /**
     * Update hidden inputs for DTO binding
     */
    function updateHiddenInputs() {
        if (selectedModels.length > 0) {
            const firstModel = selectedModels[0];
            if (selectedModelBaseModel) selectedModelBaseModel.value = firstModel.baseModel;
            if (selectedModelName) selectedModelName.value = firstModel.name;
            if (selectedModelValue) selectedModelValue.value = firstModel.name;
        } else {
            if (selectedModelBaseModel) selectedModelBaseModel.value = '';
            if (selectedModelName) selectedModelName.value = '';
            if (selectedModelValue) selectedModelValue.value = '';
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
        reset
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModelSelectionPanel;
}