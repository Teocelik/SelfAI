///**
// * Face Lock Panel Module
// * Handles face lock functionality including panel toggle, image upload, and preview
// */

//const FaceLockPanel = (function () {
//    'use strict';

//    // DOM Elements
//    let faceLockBtn = null;
//    let faceLockPanel = null;
//    let closeFaceLockPanelBtn = null;
//    let faceLockPanelUploadArea = null;
//    let faceLockPanelImage = null;
//    let faceLockPanelUploadContent = null;
//    let faceLockPanelPreview = null;
//    let faceLockPanelPreviewImg = null;
//    let removeFaceLockPanelImage = null;
//    let faceLockToggle = null;
//    let faceLockHiddenInput = null;

//    // State
//    let isPanelOpen = false;

//    /**
//     * Initialize the face lock panel
//     */
//    function init() {
//        cacheElements();
//        bindEvents();
//    }

//    /**
//     * Cache DOM elements
//     */
//    function cacheElements() {
//        faceLockBtn = document.getElementById('faceLockBtn');
//        faceLockPanel = document.getElementById('faceLockPanel');
//        closeFaceLockPanelBtn = document.getElementById('closeFaceLockPanelBtn');
//        faceLockPanelUploadArea = document.getElementById('faceLockPanelUploadArea');
//        faceLockPanelImage = document.getElementById('faceLockPanelImage');
//        faceLockPanelUploadContent = document.getElementById('faceLockPanelUploadContent');
//        faceLockPanelPreview = document.getElementById('faceLockPanelPreview');
//        faceLockPanelPreviewImg = document.getElementById('faceLockPanelPreviewImg');
//        removeFaceLockPanelImage = document.getElementById('removeFaceLockPanelImage');
//        faceLockToggle = document.getElementById('faceLockToggle');
//        faceLockHiddenInput = document.getElementById('faceLockImage');
//    }

//    /**
//     * Bind event listeners
//     */
//    function bindEvents() {
//        // Toggle button
//        if (faceLockBtn) {
//            faceLockBtn.addEventListener('click', handleToggleClick);
//        }

//        // Close button
//        if (closeFaceLockPanelBtn) {
//            closeFaceLockPanelBtn.addEventListener('click', close);
//        }

//        // Upload area
//        if (faceLockPanelUploadArea) {
//            faceLockPanelUploadArea.addEventListener('click', handleUploadAreaClick);
//            faceLockPanelUploadArea.addEventListener('dragover', handleDragOver);
//            faceLockPanelUploadArea.addEventListener('dragleave', handleDragLeave);
//            faceLockPanelUploadArea.addEventListener('drop', handleDrop);
//        }

//        // File input
//        if (faceLockPanelImage) {
//            faceLockPanelImage.addEventListener('change', handleFileSelect);
//        }

//        // Remove button
//        if (removeFaceLockPanelImage) {
//            removeFaceLockPanelImage.addEventListener('click', handleRemoveImage);
//        }

//        // Close on outside click
//        document.addEventListener('click', handleOutsideClick);

//        // Close on Escape key
//        document.addEventListener('keydown', handleEscapeKey);
//    }

//    /**
//     * Handle toggle button click
//     */
//    function handleToggleClick(e) {
//        e.preventDefault();
//        if (isPanelOpen) {
//            close();
//        } else {
//            open();
//        }
//    }

//    /**
//     * Open the panel
//     */
//    function open() {
//        if (isPanelOpen || !faceLockPanel) return;
//        isPanelOpen = true;

//        faceLockPanel.classList.remove('hidden');
//        if (faceLockBtn) faceLockBtn.classList.add('active');

//        setTimeout(() => {
//            faceLockPanel.classList.add('open');
//        }, 10);
//    }

//    /**
//     * Close the panel
//     */
//    function close() {
//        if (!isPanelOpen || !faceLockPanel) return;
//        isPanelOpen = false;

//        faceLockPanel.classList.remove('open');
//        if (faceLockBtn) faceLockBtn.classList.remove('active');

//        setTimeout(() => {
//            faceLockPanel.classList.add('hidden');
//        }, 300);
//    }

//    /**
//     * Handle upload area click
//     */
//    function handleUploadAreaClick(e) {
//        if (!e.target.closest('#removeFaceLockPanelImage') && faceLockPanelImage) {
//            faceLockPanelImage.click();
//        }
//    }

//    /**
//     * Handle drag over
//     */
//    function handleDragOver(e) {
//        e.preventDefault();
//        if (faceLockPanelUploadArea) {
//            faceLockPanelUploadArea.classList.add('drag-over');
//        }
//    }

//    /**
//     * Handle drag leave
//     */
//    function handleDragLeave() {
//        if (faceLockPanelUploadArea) {
//            faceLockPanelUploadArea.classList.remove('drag-over');
//        }
//    }

//    /**
//     * Handle drop
//     */
//    function handleDrop(e) {
//        e.preventDefault();
//        if (faceLockPanelUploadArea) {
//            faceLockPanelUploadArea.classList.remove('drag-over');
//        }

//        const files = e.dataTransfer.files;
//        if (files.length > 0) {
//            processFile(files[0]);
//        }
//    }

//    /**
//     * Handle file select
//     */
//    function handleFileSelect(e) {
//        if (e.target.files.length > 0) {
//            processFile(e.target.files[0]);
//        }
//    }

//    /**
//     * Process uploaded file
//     */
//    function processFile(file) {
//        if (!file.type.startsWith('image/')) {
//            console.warn('Invalid file type. Please upload an image.');
//            return;
//        }

//        const reader = new FileReader();
//        reader.onload = (e) => {
//            // Update preview
//            if (faceLockPanelPreviewImg) {
//                faceLockPanelPreviewImg.src = e.target.result;
//            }
//            if (faceLockPanelUploadContent) {
//                faceLockPanelUploadContent.classList.add('hidden');
//            }
//            if (faceLockPanelPreview) {
//                faceLockPanelPreview.classList.remove('hidden');
//            }

//            // Update hidden input for form submission
//            if (faceLockHiddenInput) {
//                const dataTransfer = new DataTransfer();
//                dataTransfer.items.add(file);
//                faceLockHiddenInput.files = dataTransfer.files;
//            }
//        };
//        reader.readAsDataURL(file);
//    }

//    /**
//     * Handle remove image click
//     */
//    function handleRemoveImage(e) {
//        e.stopPropagation();
//        clearImage();
//    }

//    /**
//     * Clear uploaded image
//     */
//    function clearImage() {
//        if (faceLockPanelImage) faceLockPanelImage.value = '';
//        if (faceLockPanelUploadContent) faceLockPanelUploadContent.classList.remove('hidden');
//        if (faceLockPanelPreview) faceLockPanelPreview.classList.add('hidden');
//        if (faceLockHiddenInput) faceLockHiddenInput.value = '';
//    }

//    /**
//     * Handle outside click
//     */
//    function handleOutsideClick(e) {
//        if (isPanelOpen &&
//            faceLockPanel &&
//            !faceLockPanel.contains(e.target) &&
//            faceLockBtn &&
//            !faceLockBtn.contains(e.target)) {
//            close();
//        }
//    }

//    /**
//     * Handle Escape key
//     */
//    function handleEscapeKey(e) {
//        if (e.key === 'Escape' && isPanelOpen) {
//            close();
//        }
//    }

//    /**
//     * Check if panel is open
//     * @returns {boolean}
//     */
//    function isOpen() {
//        return isPanelOpen;
//    }

//    /**
//     * Reset the panel
//     */
//    function reset() {
//        clearImage();
//        close();
//    }

//    // Public API
//    return {
//        init,
//        open,
//        close,
//        isOpen,
//        clearImage,
//        reset
//    };
//})();

//// Export for module systems
//if (typeof module !== 'undefined' && module.exports) {
//    module.exports = FaceLockPanel;
//}







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
    let faceLockAssetIdInput = null; // 🆕 Asset ID hidden input

    // State
    let isPanelOpen = false;
    let isUploading = false; // 🆕 Upload durumu
    let currentAssetId = null; // 🆕 Mevcut asset ID

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
        faceLockAssetIdInput = document.getElementById('faceLockAssetId'); // 🆕
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
        if (!e.target.closest('#removeFaceLockPanelImage') && faceLockPanelImage && !isUploading) {
            faceLockPanelImage.click();
        }
    }

    /**
     * Handle drag over
     */
    function handleDragOver(e) {
        e.preventDefault();
        if (faceLockPanelUploadArea && !isUploading) {
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

        if (isUploading) return;

        const files = e.dataTransfer.files;
        if (files.length > 0) {
            processFile(files[0]);
        }
    }

    /**
     * Handle file select
     */
    function handleFileSelect(e) {
        if (e.target.files.length > 0 && !isUploading) {
            processFile(e.target.files[0]);
        }
    }

    /**
     * 🆕 Process uploaded file - UPDATED
     */
    function processFile(file) {
        if (!file.type.startsWith('image/')) {
            showNotification('Lütfen bir görsel dosyası yükleyin.', 'error');
            return;
        }

        // Dosya boyutu kontrolü (örn: max 10MB)
        const maxSize = 10 * 1024 * 1024;
        if (file.size > maxSize) {
            showNotification('Dosya boyutu 10MB\'dan küçük olmalıdır.', 'error');
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

        // 🆕 Arka planda Asset ID al
        uploadAssetAndGetId(file);
    }

    /**
     * 🆕 Upload asset to server and get Asset ID
     */
    async function uploadAssetAndGetId(file) {
        if (isUploading) return;

        isUploading = true;
        showUploadingState(true);

        try {
            const formData = new FormData();
            formData.append('formFile', file);

            const response = await fetch('/RenderNet/GetAssetId', {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success && result.assetId) {
                currentAssetId = result.assetId;

                // Hidden input'a asset ID'yi kaydet
                if (faceLockAssetIdInput) {
                    faceLockAssetIdInput.value = result.assetId;
                }

                showNotification('Yüz görseli başarıyla yüklendi!', 'success');
                console.log('Asset ID alındı:', result.assetId);

                // Face Lock butonuna başarı göstergesi ekle
                updateFaceLockButtonState('success');
            } else {
                throw new Error(result.message || 'Asset ID alınamadı.');
            }

        } catch (error) {
            console.error('Asset upload error:', error);
            showNotification('Görsel yüklenirken hata oluştu: ' + error.message, 'error');

            // Hata durumunda preview'ı kaldırma, kullanıcı tekrar deneyebilir
            updateFaceLockButtonState('error');

            // Asset ID'yi temizle
            currentAssetId = null;
            if (faceLockAssetIdInput) {
                faceLockAssetIdInput.value = '';
            }
        } finally {
            isUploading = false;
            showUploadingState(false);
        }
    }

    /**
     * 🆕 Show uploading state in UI
     */
    function showUploadingState(uploading) {
        if (faceLockPanelUploadArea) {
            if (uploading) {
                faceLockPanelUploadArea.classList.add('uploading');
                faceLockPanelUploadArea.style.pointerEvents = 'none';

                // Loading spinner ekle
                let spinner = faceLockPanelUploadArea.querySelector('.upload-spinner');
                if (!spinner) {
                    spinner = document.createElement('div');
                    spinner.className = 'upload-spinner';
                    spinner.innerHTML = `
                        <div class="flex items-center justify-center absolute inset-0 bg-black/50 rounded-lg z-10">
                            <div class="flex flex-col items-center">
                                <div class="w-6 h-6 border-2 border-[#00CED1] border-t-transparent rounded-full animate-spin"></div>
                                <span class="text-xs text-white mt-2">Yükleniyor...</span>
                            </div>
                        </div>
                    `;
                    faceLockPanelUploadArea.style.position = 'relative';
                    faceLockPanelUploadArea.appendChild(spinner);
                }
            } else {
                faceLockPanelUploadArea.classList.remove('uploading');
                faceLockPanelUploadArea.style.pointerEvents = '';

                // Spinner'ı kaldır
                const spinner = faceLockPanelUploadArea.querySelector('.upload-spinner');
                if (spinner) {
                    spinner.remove();
                }
            }
        }
    }

    /**
     * 🆕 Update Face Lock button state
     */
    function updateFaceLockButtonState(state) {
        if (!faceLockBtn) return;

        // Önceki state class'larını kaldır
        faceLockBtn.classList.remove('face-lock-success', 'face-lock-error', 'face-lock-uploading');

        switch (state) {
            case 'success':
                faceLockBtn.classList.add('face-lock-success');
                // Yeşil nokta göster
                let successIndicator = faceLockBtn.querySelector('.status-indicator');
                if (!successIndicator) {
                    successIndicator = document.createElement('span');
                    successIndicator.className = 'status-indicator absolute -top-1 -right-1 w-3 h-3 bg-green-500 rounded-full border-2 border-surface';
                    faceLockBtn.style.position = 'relative';
                    faceLockBtn.appendChild(successIndicator);
                }
                successIndicator.classList.remove('bg-red-500');
                successIndicator.classList.add('bg-green-500');
                break;

            case 'error':
                faceLockBtn.classList.add('face-lock-error');
                let errorIndicator = faceLockBtn.querySelector('.status-indicator');
                if (!errorIndicator) {
                    errorIndicator = document.createElement('span');
                    errorIndicator.className = 'status-indicator absolute -top-1 -right-1 w-3 h-3 bg-red-500 rounded-full border-2 border-surface';
                    faceLockBtn.style.position = 'relative';
                    faceLockBtn.appendChild(errorIndicator);
                }
                errorIndicator.classList.remove('bg-green-500');
                errorIndicator.classList.add('bg-red-500');
                break;

            case 'none':
            default:
                const indicator = faceLockBtn.querySelector('.status-indicator');
                if (indicator) {
                    indicator.remove();
                }
                break;
        }
    }

    /**
     * 🆕 Show notification toast
     */
    function showNotification(message, type = 'info') {
        // Mevcut notification varsa kaldır
        const existingNotification = document.querySelector('.face-lock-notification');
        if (existingNotification) {
            existingNotification.remove();
        }

        const notification = document.createElement('div');
        notification.className = `face-lock-notification fixed bottom-4 right-4 px-4 py-3 rounded-lg shadow-lg z-50 flex items-center space-x-2 transform transition-all duration-300 translate-y-full opacity-0`;

        // Type'a göre renk belirle
        const colors = {
            success: 'bg-green-600 text-white',
            error: 'bg-red-600 text-white',
            info: 'bg-blue-600 text-white',
            warning: 'bg-yellow-600 text-white'
        };

        const icons = {
            success: 'fas fa-check-circle',
            error: 'fas fa-exclamation-circle',
            info: 'fas fa-info-circle',
            warning: 'fas fa-exclamation-triangle'
        };

        notification.classList.add(...colors[type].split(' '));
        notification.innerHTML = `
            <i class="${icons[type]}"></i>
            <span class="text-sm">${message}</span>
        `;

        document.body.appendChild(notification);

        // Animasyonla göster
        requestAnimationFrame(() => {
            notification.classList.remove('translate-y-full', 'opacity-0');
        });

        // 4 saniye sonra kaldır
        setTimeout(() => {
            notification.classList.add('translate-y-full', 'opacity-0');
            setTimeout(() => {
                notification.remove();
            }, 300);
        }, 4000);
    }

    /**
     * Handle remove image click - UPDATED
     */
    function handleRemoveImage(e) {
        e.stopPropagation();
        clearImage();
    }

    /**
     * Clear uploaded image - UPDATED
     */
    function clearImage() {
        if (faceLockPanelImage) faceLockPanelImage.value = '';
        if (faceLockPanelUploadContent) faceLockPanelUploadContent.classList.remove('hidden');
        if (faceLockPanelPreview) faceLockPanelPreview.classList.add('hidden');
        if (faceLockHiddenInput) faceLockHiddenInput.value = '';

        // 🆕 Asset ID'yi temizle
        currentAssetId = null;
        if (faceLockAssetIdInput) {
            faceLockAssetIdInput.value = '';
        }

        // 🆕 Button state'ini sıfırla
        updateFaceLockButtonState('none');
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
     * 🆕 Get current asset ID
     * @returns {string|null}
     */
    function getAssetId() {
        return currentAssetId;
    }

    /**
     * 🆕 Check if upload is in progress
     * @returns {boolean}
     */
    function isUploadInProgress() {
        return isUploading;
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
        reset,
        getAssetId,        // 🆕
        isUploadInProgress // 🆕
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FaceLockPanel;
}