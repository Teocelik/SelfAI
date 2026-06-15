/**
 * Pose Lock Panel Module (F.5b — Full-Screen Modal)
 * Poz referans görseli seçimini yönetir. F.5b'de sidebar slide-in panel,
 * full-screen modal'a (#poseLockModal) dönüştürüldü:
 *   - Studio'daki Pose Lock butonu (#poseLockBtn) modal'ı açar.
 *   - Kapatma: X butonu / backdrop (data-pose-modal-close) / ESC.
 *   - Body scroll lock (.is-pose-modal-open) modal açıkken.
 *
 * ASSET UPLOAD AKIŞI DEĞİŞMEDİ:
 *   dosya → /RenderNet/GetAssetId → asset_id → hidden input (#poseLockAssetId)
 *   + Studio butonu thumbnail swap. Aynı dosya input ID'si (#poseLockPanelImage)
 *   korundu; manuel upload ve preset (uploadFromUrl) akışları aynı.
 *
 * ORTHOGONAL: Pose Lock, Character ↔ Face Lock mutual exclusivity'sine DOKUNMAZ.
 * Pose görseli seçili olsa bile diğer iki buton/panel durumunu değiştirmez; üçü
 * birlikte aktif olabilir (backend control_net'i facelock/character ile birlikte gönderir).
 */

const PoseLockPanel = (function () {
    'use strict';

    // ─── DOM Referansları ───
    let poseLockBtn = null;   // Studio'daki tool button (Consistency Controls)
    let modal = null;         // #poseLockModal (full-screen)
    let fileInput = null;     // #poseLockPanelImage (modal içinde, ID korundu)
    let uploadZone = null;    // .pose-modal__upload-zone (spinner overlay için)

    // ─── Modül State ───
    let currentAssetId = null;   // yüklenmiş görselin asset_id'si (string|null)
    let isUploading = false;

    const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB (Face Lock ile aynı sınır)

    /**
     * Modülü başlat (App.js → initPoseLockPanel çağırır)
     */
    function init() {
        cacheElements();
        bindEvents();
        console.log('[PoseLockPanel] initialized (modal)');
    }

    /**
     * DOM referanslarını al
     */
    function cacheElements() {
        poseLockBtn = document.getElementById('poseLockBtn');
        modal = document.getElementById('poseLockModal');
        fileInput = document.getElementById('poseLockPanelImage');
        uploadZone = modal ? modal.querySelector('.pose-modal__upload-zone') : null;
    }

    /**
     * Event listener'ları bağla
     */
    function bindEvents() {
        // Studio'daki Pose Lock butonu → modal aç
        if (poseLockBtn) {
            poseLockBtn.addEventListener('click', handleBtnClick);
        }

        // Modal kapatma elemanları (X butonu + backdrop)
        if (modal) {
            modal.querySelectorAll('[data-pose-modal-close]').forEach(function (el) {
                el.addEventListener('click', closePoseModal);
            });
        }

        // Dosya seçimi (manuel upload)
        if (fileInput) {
            fileInput.addEventListener('change', handleFileSelect);
        }

        // ESC ile kapat
        document.addEventListener('keydown', handleEscapeKey);
    }

    // ═══════════════════════════════════════════════
    // MODAL AÇ / KAPAT
    // ═══════════════════════════════════════════════

    function handleBtnClick(e) {
        e.preventDefault();
        openPoseModal();
    }

    function openPoseModal() {
        if (!modal) return;
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('is-pose-modal-open');

        // Focus management — kapat butonuna odaklan
        setTimeout(function () {
            const closeBtn = modal.querySelector('.pose-modal__close');
            if (closeBtn) closeBtn.focus();
        }, 100);
    }

    function closePoseModal() {
        if (!modal) return;
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('is-pose-modal-open');
    }

    function handleEscapeKey(e) {
        if (e.key === 'Escape' && isOpen()) {
            closePoseModal();
        }
    }

    function isOpen() {
        return !!(modal && modal.classList.contains('is-open'));
    }

    // ═══════════════════════════════════════════════
    // GÖRSEL UPLOAD (Face Lock akışıyla aynı: dosya → /GetAssetId → asset_id)
    // ═══════════════════════════════════════════════

    /**
     * Dosya seçildiğinde — yükle, başarılıysa modal'ı kapat.
     */
    async function handleFileSelect(e) {
        const file = e.target.files && e.target.files[0];
        if (!file || isUploading) return;

        await uploadAndApply(file);

        // Modal içinde preview alanı yok; başarılı seçimden sonra modal kapanır.
        if (currentAssetId) {
            closePoseModal();
        }
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
     * Dosyayı /RenderNet/GetAssetId'e yükle, başarılıysa hidden input + buton
     * thumbnail güncelle. Face Lock'un akışıyla birebir (multipart 'formFile' alanı).
     */
    async function uploadAndApply(file, options) {
        // silentToast: dış API (preset akışı) kendi Toast'unu gösterdiği için
        // buradaki başarı Toast'u bastırılabilir. Varsayılan davranış DEĞİŞMEZ.
        const silentToast = !!(options && options.silentToast);

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

                // Preview için dosyayı data URL'e çevir → buton thumbnail swap
                const dataUrl = await readFileAsDataUrl(file);
                updatePoseLockBtnThumbnail(dataUrl);

                if (!silentToast) {
                    Toast.success('Poz görseli başarıyla yüklendi!', 'Pose Lock');
                }
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
     * Yükleme sırasında upload zone'a spinner overlay göster (Face Lock stili)
     */
    function showUploadingState(uploading) {
        if (!uploadZone) return;

        if (uploading) {
            uploadZone.style.pointerEvents = 'none';
            uploadZone.style.position = 'relative';

            let spinner = uploadZone.querySelector('.upload-spinner');
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
                uploadZone.appendChild(spinner);
            }
        } else {
            uploadZone.style.pointerEvents = '';
            const spinner = uploadZone.querySelector('.upload-spinner');
            if (spinner) spinner.remove();
        }
    }

    /**
     * Seçili poz görselini temizle: state, hidden input ve buton icon'una dön.
     */
    function clearImage() {
        currentAssetId = null;
        setHiddenValue('poseLockAssetId', '');
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
    // YARDIMCILAR
    // ═══════════════════════════════════════════════

    function setHiddenValue(id, value) {
        const el = document.getElementById(id);
        if (el) el.value = value;
    }

    /**
     * PUBLIC: Bir URL'deki görseli (örn. preset şablon) mevcut upload akışına sokar.
     * Görsel blob olarak indirilir, File'a çevrilir ve MEVCUT uploadAndApply akışı
     * (validasyon → /RenderNet/GetAssetId → hidden input + buton thumbnail)
     * yeniden kullanılır. Tek fark: başarı Toast'u bastırılır (çağıran taraf gösterir).
     *
     * @param {string} presetUrl - İndirilecek görselin URL'si
     * @param {string} presetName - Kullanıcıya gösterilecek poz adı (loglama için)
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    async function uploadFromUrl(presetUrl, presetName) {
        if (!presetUrl) {
            return { success: false, error: 'Geçersiz preset adresi.' };
        }
        if (isUploading) {
            return { success: false, error: 'Şu anda başka bir yükleme devam ediyor.' };
        }

        try {
            const response = await fetch(presetUrl);
            if (!response.ok) {
                throw new Error('Preset görsel bulunamadı.');
            }

            const blob = await response.blob();
            const fileName = presetUrl.split('/').pop() || 'preset.jpg';
            const file = new File([blob], fileName, { type: blob.type || 'image/jpeg' });

            // MEVCUT akışı yeniden kullan (hidden input, thumbnail hepsi içeride).
            await uploadAndApply(file, { silentToast: true });

            // uploadAndApply başarıda currentAssetId set eder, hatada temizler.
            if (currentAssetId) {
                console.log('[PoseLockPanel] Preset uygulandı: %s', presetName);
                return { success: true };
            }
            return { success: false, error: 'Asset ID alınamadı.' };
        } catch (error) {
            console.error('[PoseLockPanel] Preset yükleme hatası:', error);
            return { success: false, error: error.message };
        }
    }

    /**
     * PUBLIC: Yüklü asset_id'yi döndür (debug / dış kontrol için)
     */
    function getCurrentAssetId() {
        return currentAssetId;
    }

    // Public API
    return {
        init,
        updatePoseLockBtnThumbnail,
        uploadFromUrl,
        getCurrentAssetId,
        isOpen,
        open: openPoseModal,
        close: closePoseModal,
        clearImage
    };
})();

// pose-presets.js gibi dış modüllerin erişebilmesi için window'a expose et
// (const top-level olduğu için window'a otomatik bağlanmaz).
if (typeof window !== 'undefined') {
    window.PoseLockPanel = PoseLockPanel;
}

// Modül sistemleri için export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PoseLockPanel;
}
