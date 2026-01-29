/**
 * Flux Image Styles Panel Module
 * Handles style selection, image upload, and intensity control
 */

const FluxImageStyles = (function () {
    'use strict';

    // DOM Elements
    let fluxImageStyleBtn = null;
    let fluxStyleSelectedBtn = null;
    let fluxStylePanel = null;
    let fluxStyleArrow = null;
    let fluxStyleArrowSelected = null;
    let closeFluxPanelBtn = null;
    let styleOptions = null;
    let selectedStyleValue = null;
    let selectedStylePreview = null;
    let removeSelectedStyle = null;
    let styleIntensity = null;
    let intensityValue = null;
    let fluxStyleDefault = null;
    let fluxStyleSelected = null;

    // Upload elements
    let styleUploadArea = null;
    let styleImageInput = null;
    let styleUploadContent = null;
    let styleUploadPreview = null;
    let styleUploadPreviewImg = null;
    let removeUploadedStyle = null;

    // State
    let isPanelOpen = false;
    let selectedStyle = null;

    /**
     * Initialize the flux image styles panel
     */
    function init() {
        cacheElements();
        bindEvents();
        initializeIntensitySlider();
    }

    /**
     * Cache DOM elements
     */
    function cacheElements() {
        fluxImageStyleBtn = document.getElementById('fluxImageStyleBtn');
        fluxStyleSelectedBtn = document.getElementById('fluxStyleSelectedBtn');
        fluxStylePanel = document.getElementById('fluxStylePanel');
        fluxStyleArrow = document.getElementById('fluxStyleArrow');
        fluxStyleArrowSelected = document.getElementById('fluxStyleArrowSelected');
        closeFluxPanelBtn = document.getElementById('closeFluxPanelBtn');
        styleOptions = document.querySelectorAll('.style-option');
        selectedStyleValue = document.getElementById('selectedStyleValue');
        selectedStylePreview = document.getElementById('selectedStylePreview');
        removeSelectedStyle = document.getElementById('removeSelectedStyle');
        styleIntensity = document.getElementById('styleIntensity');
        intensityValue = document.getElementById('intensityValue');
        fluxStyleDefault = document.getElementById('fluxStyleDefault');
        fluxStyleSelected = document.getElementById('fluxStyleSelected');

        // Upload elements
        styleUploadArea = document.getElementById('styleUploadArea');
        styleImageInput = document.getElementById('styleImageInput');
        styleUploadContent = document.getElementById('styleUploadContent');
        styleUploadPreview = document.getElementById('styleUploadPreview');
        styleUploadPreviewImg = document.getElementById('styleUploadPreviewImg');
        removeUploadedStyle = document.getElementById('removeUploadedStyle');
    }

    /**
     * Bind event listeners
     */
    function bindEvents() {
        // Toggle buttons
        if (fluxImageStyleBtn) {
            fluxImageStyleBtn.addEventListener('click', handleToggleClick);
        }
        if (fluxStyleSelectedBtn) {
            fluxStyleSelectedBtn.addEventListener('click', handleToggleClick);
        }

        // Close button
        if (closeFluxPanelBtn) {
            closeFluxPanelBtn.addEventListener('click', close);
        }

        // Style options
        if (styleOptions) {
            styleOptions.forEach(option => {
                option.addEventListener('click', handleStyleOptionClick);
            });
        }

        // Remove selected style
        if (removeSelectedStyle) {
            removeSelectedStyle.addEventListener('click', handleRemoveStyle);
        }

        // Intensity slider
        if (styleIntensity) {
            styleIntensity.addEventListener('input', handleIntensityChange);
        }

        // Upload area
        if (styleUploadArea) {
            styleUploadArea.addEventListener('click', handleUploadAreaClick);
            styleUploadArea.addEventListener('dragover', handleDragOver);
            styleUploadArea.addEventListener('dragleave', handleDragLeave);
            styleUploadArea.addEventListener('drop', handleDrop);
        }

        // File input
        if (styleImageInput) {
            styleImageInput.addEventListener('change', handleFileSelect);
        }

        // Remove uploaded style
        if (removeUploadedStyle) {
            removeUploadedStyle.addEventListener('click', handleRemoveUploadedStyle);
        }

        // Close on outside click
        document.addEventListener('click', handleOutsideClick);

        // Close on Escape key
        document.addEventListener('keydown', handleEscapeKey);
    }

    /**
     * Initialize intensity slider styling
     */
    function initializeIntensitySlider() {
        if (styleIntensity) {
            styleIntensity.dispatchEvent(new Event('input'));
        }
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
        if (isPanelOpen || !fluxStylePanel) return;
        isPanelOpen = true;

        fluxStylePanel.classList.remove('hidden');

        // Rotate arrows
        if (fluxStyleArrow) fluxStyleArrow.style.transform = 'rotate(180deg)';
        if (fluxStyleArrowSelected) fluxStyleArrowSelected.style.transform = 'rotate(180deg)';

        setTimeout(() => {
            fluxStylePanel.classList.add('open');
        }, 10);
    }

    /**
     * Close the panel
     */
    function close() {
        if (!isPanelOpen || !fluxStylePanel) return;
        isPanelOpen = false;

        fluxStylePanel.classList.remove('open');

        // Reset arrows
        if (fluxStyleArrow) fluxStyleArrow.style.transform = 'rotate(0deg)';
        if (fluxStyleArrowSelected) fluxStyleArrowSelected.style.transform = 'rotate(0deg)';

        setTimeout(() => {
            fluxStylePanel.classList.add('hidden');
        }, 300);
    }

    /**
     * Update UI based on selection state
     */
    function updateUI() {
        if (!selectedStyle) {
            // No selection - show default button
            if (fluxStyleDefault) fluxStyleDefault.classList.remove('hidden');
            if (fluxStyleSelected) fluxStyleSelected.classList.add('hidden');
            if (selectedStyleValue) selectedStyleValue.value = '';
            return;
        }

        // Has selection - show selected view
        if (fluxStyleDefault) fluxStyleDefault.classList.add('hidden');
        if (fluxStyleSelected) fluxStyleSelected.classList.remove('hidden');
        if (selectedStylePreview) selectedStylePreview.src = selectedStyle.img;
        if (selectedStyleValue) selectedStyleValue.value = selectedStyle.id;
    }

    /**
     * Handle style option click
     */
    function handleStyleOptionClick() {
        const styleId = this.dataset.style;
        const styleName = this.dataset.name;
        const styleImg = this.dataset.img;

        // Remove previous selection
        if (styleOptions) {
            styleOptions.forEach(opt => opt.classList.remove('selected'));
        }

        // Add selection to clicked
        this.classList.add('selected');

        // Update selected style
        selectedStyle = {
            id: styleId,
            name: styleName,
            img: styleImg,
            isUploaded: false
        };

        updateUI();

        // Close panel after selection
        setTimeout(() => {
            close();
        }, 200);
    }

    /**
     * Handle remove style click
     */
    function handleRemoveStyle(e) {
        e.stopPropagation();
        selectedStyle = null;

        if (styleOptions) {
            styleOptions.forEach(opt => opt.classList.remove('selected'));
        }

        updateUI();

        // Reset upload if it was uploaded
        if (styleUploadContent) styleUploadContent.classList.remove('hidden');
        if (styleUploadPreview) styleUploadPreview.classList.add('hidden');
        if (styleImageInput) styleImageInput.value = '';
    }

    /**
     * Handle intensity slider change
     */
    function handleIntensityChange() {
        const value = this.value;
        if (intensityValue) intensityValue.textContent = value;

        // Update slider background
        const percentage = ((value - 1) / 9) * 100;
        this.style.background = `linear-gradient(to right, #6366f1 ${percentage}%, #2C2C2C ${percentage}%)`;
    }

    /**
     * Handle upload area click
     */
    function handleUploadAreaClick(e) {
        if (!e.target.closest('#removeUploadedStyle') && styleImageInput) {
            styleImageInput.click();
        }
    }

    /**
     * Handle drag over
     */
    function handleDragOver(e) {
        e.preventDefault();
        if (styleUploadArea) {
            styleUploadArea.classList.add('drag-over');
        }
    }

    /**
     * Handle drag leave
     */
    function handleDragLeave() {
        if (styleUploadArea) {
            styleUploadArea.classList.remove('drag-over');
        }
    }

    /**
     * Handle drop
     */
    function handleDrop(e) {
        e.preventDefault();
        if (styleUploadArea) {
            styleUploadArea.classList.remove('drag-over');
        }

        const files = e.dataTransfer.files;
        if (files.length > 0) {
            processFile(files[0]);
        }
    }

    /**
     * Handle file select
     */
    function handleFileSelect(e) {
        if (e.target.files.length > 0) {
            processFile(e.target.files[0]);
        }
    }

    /**
     * Process uploaded file
     */
    function processFile(file) {
        if (!file.type.startsWith('image/')) {
            console.warn('Invalid file type. Please upload an image.');
            return;
        }

        const reader = new FileReader();
        reader.onload = (e) => {
            // Update upload area preview
            if (styleUploadPreviewImg) styleUploadPreviewImg.src = e.target.result;
            if (styleUploadContent) styleUploadContent.classList.add('hidden');
            if (styleUploadPreview) styleUploadPreview.classList.remove('hidden');

            // Remove selection from example styles
            if (styleOptions) {
                styleOptions.forEach(opt => opt.classList.remove('selected'));
            }

            // Update selected style
            selectedStyle = {
                id: 'uploaded',
                name: 'Uploaded Image',
                img: e.target.result,
                isUploaded: true
            };

            updateUI();

            // Close panel after upload
            setTimeout(() => {
                close();
            }, 300);
        };
        reader.readAsDataURL(file);
    }

    /**
     * Handle remove uploaded style click
     */
    function handleRemoveUploadedStyle(e) {
        e.stopPropagation();

        if (styleImageInput) styleImageInput.value = '';
        if (styleUploadContent) styleUploadContent.classList.remove('hidden');
        if (styleUploadPreview) styleUploadPreview.classList.add('hidden');

        // Only clear if current selection is uploaded
        if (selectedStyle && selectedStyle.isUploaded) {
            selectedStyle = null;
            updateUI();
        }
    }

    /**
     * Handle outside click
     */
    function handleOutsideClick(e) {
        if (isPanelOpen &&
            fluxStylePanel &&
            !fluxStylePanel.contains(e.target) &&
            !e.target.closest('#fluxImageStyleSection')) {
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
     * Get selected style
     * @returns {object|null}
     */
    function getSelectedStyle() {
        return selectedStyle;
    }

    /**
     * Reset the panel
     */
    function reset() {
        selectedStyle = null;

        if (styleOptions) {
            styleOptions.forEach(opt => opt.classList.remove('selected'));
        }

        if (styleUploadContent) styleUploadContent.classList.remove('hidden');
        if (styleUploadPreview) styleUploadPreview.classList.add('hidden');
        if (styleImageInput) styleImageInput.value = '';

        updateUI();
        close();
    }

    // Public API
    return {
        init,
        open,
        close,
        isOpen,
        getSelectedStyle,
        reset
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FluxImageStyles;
}
