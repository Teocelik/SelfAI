/**
 * Face Lock Panel Module
 * Handles face lock functionality including panel toggle, image upload, and preview
 */

const FaceLockPanel = (function () {
    'use strict';

    // DOM Elements
    let faceLockBtn = null;
    let faceLockPanel = null;
    let closeFaceLockPanelBtn = null;
    let faceLockPanelUploadArea = null;
    let faceLockPanelImage = null;
    let faceLockPanelUploadContent = null;
    let faceLockPanelPreview = null;
    let faceLockPanelPreviewImg = null;
    let removeFaceLockPanelImage = null;
    let faceLockToggle = null;
    let faceLockHiddenInput = null;

    // State
    let isPanelOpen = false;

    /**
     * Initialize the face lock panel
     */
    function init() {
        cacheElements();
        bindEvents();
    }

    /**
     * Cache DOM elements
     */
    function cacheElements() {
        faceLockBtn = document.getElementById('faceLockBtn');
        faceLockPanel = document.getElementById('faceLockPanel');
        closeFaceLockPanelBtn = document.getElementById('closeFaceLockPanelBtn');
        faceLockPanelUploadArea = document.getElementById('faceLockPanelUploadArea');
        faceLockPanelImage = document.getElementById('faceLockPanelImage');
        faceLockPanelUploadContent = document.getElementById('faceLockPanelUploadContent');
        faceLockPanelPreview = document.getElementById('faceLockPanelPreview');
        faceLockPanelPreviewImg = document.getElementById('faceLockPanelPreviewImg');
        removeFaceLockPanelImage = document.getElementById('removeFaceLockPanelImage');
        faceLockToggle = document.getElementById('faceLockToggle');
        faceLockHiddenInput = document.getElementById('faceLockImage');
    }

    /**
     * Bind event listeners
     */
    function bindEvents() {
        // Toggle button
        if (faceLockBtn) {
            faceLockBtn.addEventListener('click', handleToggleClick);
        }

        // Close button
        if (closeFaceLockPanelBtn) {
            closeFaceLockPanelBtn.addEventListener('click', close);
        }

        // Upload area
        if (faceLockPanelUploadArea) {
            faceLockPanelUploadArea.addEventListener('click', handleUploadAreaClick);
            faceLockPanelUploadArea.addEventListener('dragover', handleDragOver);
            faceLockPanelUploadArea.addEventListener('dragleave', handleDragLeave);
            faceLockPanelUploadArea.addEventListener('drop', handleDrop);
        }

        // File input
        if (faceLockPanelImage) {
            faceLockPanelImage.addEventListener('change', handleFileSelect);
        }

        // Remove button
        if (removeFaceLockPanelImage) {
            removeFaceLockPanelImage.addEventListener('click', handleRemoveImage);
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
        if (isPanelOpen || !faceLockPanel) return;
        isPanelOpen = true;

        faceLockPanel.classList.remove('hidden');
        if (faceLockBtn) faceLockBtn.classList.add('active');

        setTimeout(() => {
            faceLockPanel.classList.add('open');
        }, 10);
    }

    /**
     * Close the panel
     */
    function close() {
        if (!isPanelOpen || !faceLockPanel) return;
        isPanelOpen = false;

        faceLockPanel.classList.remove('open');
        if (faceLockBtn) faceLockBtn.classList.remove('active');

        setTimeout(() => {
            faceLockPanel.classList.add('hidden');
        }, 300);
    }

    /**
     * Handle upload area click
     */
    function handleUploadAreaClick(e) {
        if (!e.target.closest('#removeFaceLockPanelImage') && faceLockPanelImage) {
            faceLockPanelImage.click();
        }
    }

    /**
     * Handle drag over
     */
    function handleDragOver(e) {
        e.preventDefault();
        if (faceLockPanelUploadArea) {
            faceLockPanelUploadArea.classList.add('drag-over');
        }
    }

    /**
     * Handle drag leave
     */
    function handleDragLeave() {
        if (faceLockPanelUploadArea) {
            faceLockPanelUploadArea.classList.remove('drag-over');
        }
    }

    /**
     * Handle drop
     */
    function handleDrop(e) {
        e.preventDefault();
        if (faceLockPanelUploadArea) {
            faceLockPanelUploadArea.classList.remove('drag-over');
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
            // Update preview
            if (faceLockPanelPreviewImg) {
                faceLockPanelPreviewImg.src = e.target.result;
            }
            if (faceLockPanelUploadContent) {
                faceLockPanelUploadContent.classList.add('hidden');
            }
            if (faceLockPanelPreview) {
                faceLockPanelPreview.classList.remove('hidden');
            }

            // Update hidden input for form submission
            if (faceLockHiddenInput) {
                const dataTransfer = new DataTransfer();
                dataTransfer.items.add(file);
                faceLockHiddenInput.files = dataTransfer.files;
            }
        };
        reader.readAsDataURL(file);
    }

    /**
     * Handle remove image click
     */
    function handleRemoveImage(e) {
        e.stopPropagation();
        clearImage();
    }

    /**
     * Clear uploaded image
     */
    function clearImage() {
        if (faceLockPanelImage) faceLockPanelImage.value = '';
        if (faceLockPanelUploadContent) faceLockPanelUploadContent.classList.remove('hidden');
        if (faceLockPanelPreview) faceLockPanelPreview.classList.add('hidden');
        if (faceLockHiddenInput) faceLockHiddenInput.value = '';
    }

    /**
     * Handle outside click
     */
    function handleOutsideClick(e) {
        if (isPanelOpen &&
            faceLockPanel &&
            !faceLockPanel.contains(e.target) &&
            faceLockBtn &&
            !faceLockBtn.contains(e.target)) {
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
     * Reset the panel
     */
    function reset() {
        clearImage();
        close();
    }

    // Public API
    return {
        init,
        open,
        close,
        isOpen,
        clearImage,
        reset
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FaceLockPanel;
}