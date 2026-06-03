/**
 * Pose Lock Panel Module
 * Poz referans görseli yükleme panelini yönetir: panel aç/kapat, görsel upload
 * (/RenderNet/GetAssetId), preview ve buton thumbnail swap.
 *
 * Face Lock paneliyle aynı görsel dili ve aynı .open/.hidden geçiş mantığını kullanır.
 *
 * ORTHOGONAL: Pose Lock, Character ↔ Face Lock mutual exclusivity'sine DOKUNMAZ.
 * Pose görseli seçili olsa bile diğer iki buton/panel durumunu değiştirmez; üçü
 * birlikte aktif olabilir (backend control_net'i facelock/character ile birlikte gönderir).
 * Aynı anda yalnızca tek panelin açık kalması, mevcut panellerin (Face Lock, Character)
 * kendi handleOutsideClick handler'ları sayesinde otomatik sağlanır: Pose Lock butonuna
 * tıklanınca o panellerin dışına tıklanmış olur ve kendiliğinden kapanırlar.
 */

const PoseLockPanel = (function () {
    'use strict';

    // ─── DOM Referansları ───
    let poseLockBtn = null;
    let poseLockPanel = null;
    let closeBtn = null;
    let uploadArea = null;
    let fileInput = null;
    let uploadContent = null;
    let preview = null;
    let previewImg = null;
    let changeBtn = null;
    let assetIdInput = null;

    // ─── Modül State ───
    let currentAssetId = null;   // yüklenmiş görselin asset_id'si (string|null)
    let isPanelOpen = false;
    let isUploading = false;

    const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB (Face Lock ile aynı sınır)

    /**
     * Modülü başlat
     */
    function init() {
        cacheElements();
        bindEvents();
        console.log('[PoseLockPanel] initialized');
    }

    /**
     * DOM referanslarını al
     */
    function cacheElements() {
        poseLockBtn = document.getElementById('poseLockBtn');
        poseLockPanel = document.getElementById('poseLockPanel');
        closeBtn = document.getElementById('closePoseLockPanel');
        uploadArea = document.getElementById('poseLockPanelUploadArea');
        fileInput = document.getElementById('poseLockPanelImage');
        uploadContent = document.getElementById('poseLockPanelUploadContent');
        preview = document.getElementById('poseLockPanelPreview');
        previewImg = document.getElementById('poseLockPanelPreviewImg');
        changeBtn = document.getElementById('changePoseLockImage');
        assetIdInput = document.getElementById('poseLockAssetId');
    }

    /**
     * Event listener'ları bağla
     */
    function bindEvents() {
        if (poseLockBtn) {
            poseLockBtn.addEventListener('click', handleBtnClick);
        }
        if (closeBtn) {
            closeBtn.addEventListener('click', closePanel);
        }
        if (uploadArea) {
            uploadArea.addEventListener('click', handleUploadAreaClick);
        }
        if (fileInput) {
            fileInput.addEventListener('change', handleFileSelect);
        }
        if (changeBtn) {
            changeBtn.addEventListener('click', handleChangeImage);
        }

        // Panel dışına tıklayınca kapat
        document.addEventListener('click', handleOutsideClick);
        // Escape ile kapat
        document.addEventListener('keydown', handleEscapeKey);
    }

    // ═══════════════════════════════════════════════
    // GÖRSEL UPLOAD (Face Lock akışıyla aynı: dosya → /GetAssetId → asset_id)
    // ═══════════════════════════════════════════════

    /**
     * Upload alanına tıklama — dosya seçiciyi aç (Change butonu ve yükleme sırasında hariç)
     */
    function handleUploadAreaClick(e) {
        if (e.target.closest('#changePoseLockImage')) return;
        if (isUploading) return;
        if (fileInput) fileInput.click();
    }

    /**
     * Dosya seçildiğinde
     */
    function handleFileSelect(e) {
        const file = e.target.files && e.target.files[0];
        if (!file || isUploading) return;
        uploadAndApply(file);
    }

    /**
     * Dosya validasyonu (Face Lock ile aynı: image tipi + 10MB sınırı)
     */
    function validateFile(file) {
        if (!file.type.startsWith('image/')) {
            Toast.error('Lütfen bir görsel dosyası yükleyin.', 'Geçersiz dosya');
            return false;
        }
        if (file.size > MAX_FILE_SIZE) {
            Toast.error("Dosya boyutu 10MB'dan küçük olmalıdır.", 'Dosya çok büyük');
            return false;
        }
        return true;
    }

    /**
     * Dosyayı /RenderNet/GetAssetId'e yükle, başarılıysa preview + thumbnail + hidden input güncelle.
     * Face Lock'un uploadAssetAndGetId akışıyla birebir (multipart 'formFile' alanı).
     */
    async function uploadAndApply(file) {
        if (!validateFile(file)) {
            if (fileInput) fileInput.value = '';
            return;
        }

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
                throw new Error(`HTTP ${response.status}`);
            }

            const result = await response.json();

            if (result.success && result.assetId) {
                currentAssetId = result.assetId;
                setHiddenValue('poseLockAssetId', result.assetId);

                // Preview için dosyayı data URL'e çevir
                const dataUrl = await readFileAsDataUrl(file);
                if (previewImg) previewImg.src = dataUrl;
                if (uploadContent) uploadContent.classList.add('hidden');
                if (preview) preview.classList.remove('hidden');

                // Buton thumbnail swap
                updatePoseLockBtnThumbnail(dataUrl);

                Toast.success('Poz görseli başarıyla yüklendi!', 'Pose Lock');
                console.log('[PoseLockPanel] Asset ID alındı: %s', result.assetId);
            } else {
                throw new Error(result.message || 'Asset ID alınamadı.');
            }
        } catch (error) {
            console.error('[PoseLockPanel] Görsel yükleme hatası:', error);
            Toast.error('Poz görseli yüklenirken bir hata oluştu. Lütfen tekrar deneyin.', 'Pose Lock');
            // Hatada state'i temizle ki tutarsız kalmasın
            clearImage();
        } finally {
            isUploading = false;
            showUploadingState(false);
            if (fileInput) fileInput.value = '';
        }
    }

    /**
     * FileReader'ı promise'e saran yardımcı
     */
    function readFileAsDataUrl(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result);
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }

    /**
     * Yükleme sırasında upload alanına spinner overlay göster (Face Lock stili)
     */
    function showUploadingState(uploading) {
        if (!uploadArea) return;

        if (uploading) {
            uploadArea.style.pointerEvents = 'none';
            uploadArea.style.position = 'relative';

            let spinner = uploadArea.querySelector('.upload-spinner');
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
                uploadArea.appendChild(spinner);
            }
        } else {
            uploadArea.style.pointerEvents = '';
            const spinner = uploadArea.querySelector('.upload-spinner');
            if (spinner) spinner.remove();
        }
    }

    /**
     * Change Image — mevcut seçimi temizle ve yeniden dosya seçiciyi aç
     */
    function handleChangeImage(e) {
        e.preventDefault();
        e.stopPropagation();
        clearImage();
        if (fileInput) fileInput.click();
    }

    /**
     * Seçili poz görselini temizle: state, hidden input, preview ve buton icon'una dön.
     */
    function clearImage() {
        currentAssetId = null;
        setHiddenValue('poseLockAssetId', '');

        if (preview) preview.classList.add('hidden');
        if (uploadContent) uploadContent.classList.remove('hidden');
        if (previewImg) previewImg.src = '';
        if (fileInput) fileInput.value = '';

        updatePoseLockBtnThumbnail(null);
    }

    // ═══════════════════════════════════════════════
    // BUTON THUMBNAIL (face-lock-panel.js::updateButtonThumbnail birebir adaptasyonu)
    // ═══════════════════════════════════════════════

    /**
     * Buton thumbnail'ını Face Lock stili güncelle.
     * imageSrc verilirse: ikon gizlenir, butona sabit boyutlu thumbnail eklenir.
     * imageSrc null verilirse: thumbnail kaldırılır, ikon geri gelir.
     */
    function updatePoseLockBtnThumbnail(imageSrc) {
        if (!poseLockBtn) return;

        const buttonContent = poseLockBtn.querySelector('.flex.flex-col');
        if (!buttonContent) return;

        const icon = buttonContent.querySelector('i.fa-running');
        let thumbnail = buttonContent.querySelector('.pose-lock-btn-thumbnail');

        if (imageSrc) {
            if (icon) icon.style.display = 'none';

            if (!thumbnail) {
                thumbnail = document.createElement('div');
                thumbnail.className = 'pose-lock-btn-thumbnail';
                thumbnail.innerHTML = `
                    <img src="" alt="Pose Lock" class="w-10 h-10 object-cover rounded-lg border-2 border-primary/50">
                `;
                buttonContent.insertBefore(thumbnail, buttonContent.firstChild);
            }

            const thumbnailImg = thumbnail.querySelector('img');
            if (thumbnailImg) thumbnailImg.src = imageSrc;
        } else {
            if (thumbnail) thumbnail.remove();
            if (icon) icon.style.display = '';
        }
    }

    // ═══════════════════════════════════════════════
    // PANEL AÇ / KAPAT (Face Lock + Character paneliyle aynı mantık)
    // ═══════════════════════════════════════════════

    function handleBtnClick(e) {
        e.preventDefault();
        togglePanel();
    }

    function togglePanel() {
        if (isPanelOpen) {
            closePanel();
        } else {
            openPanel();
        }
    }

    function openPanel() {
        if (isPanelOpen || !poseLockPanel) return;
        isPanelOpen = true;

        poseLockPanel.classList.remove('hidden');
        if (poseLockBtn) poseLockBtn.classList.add('active');

        setTimeout(() => {
            poseLockPanel.classList.add('open');
        }, 10);
    }

    function closePanel() {
        if (!isPanelOpen || !poseLockPanel) return;
        isPanelOpen = false;

        poseLockPanel.classList.remove('open');
        if (poseLockBtn) poseLockBtn.classList.remove('active');

        setTimeout(() => {
            poseLockPanel.classList.add('hidden');
        }, 300);
    }

    function handleOutsideClick(e) {
        if (
            isPanelOpen &&
            poseLockPanel &&
            !poseLockPanel.contains(e.target) &&
            poseLockBtn &&
            !poseLockBtn.contains(e.target)
        ) {
            closePanel();
        }
    }

    function handleEscapeKey(e) {
        if (e.key === 'Escape' && isPanelOpen) {
            closePanel();
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
     * PUBLIC: Yüklü asset_id'yi döndür (debug / dış kontrol için)
     */
    function getCurrentAssetId() {
        return currentAssetId;
    }

    function isOpen() {
        return isPanelOpen;
    }

    // Public API
    return {
        init,
        updatePoseLockBtnThumbnail,
        getCurrentAssetId,
        isOpen,
        close: closePanel,
        clearImage
    };
})();

// Modül sistemleri için export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PoseLockPanel;
}
