/**
 * ============================================
 *  TOAST NOTIFICATION SYSTEM
 *  Kullanım: Toast.success("Mesaj"), Toast.error("Mesaj") vb.
 * ============================================
 * 
 *  🟢 Toast.success(message, title, duration)
 *  🔴 Toast.error(message, title, duration)
 *  🟡 Toast.warning(message, title, duration)
 *  🔵 Toast.info(message, title, duration)
 * 
 *  📡 apiFetch(url, options) → Fetch wrapper, otomatik toast gösterir
 * ============================================
 */

// ─── Toast Container'ı Oluştur ───
(function initToastContainer() {
    if (!document.getElementById('toastContainer')) {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        document.body.appendChild(container);
    }
})();


// ─── Icon Mapping ───
const TOAST_ICONS = {
    success: 'fa-check-circle',
    error: 'fa-times-circle',
    warning: 'fa-exclamation-triangle',
    info: 'fa-info-circle'
};

// ─── Varsayılan Başlıklar ───
const TOAST_TITLES = {
    success: 'Başarılı!',
    error: 'Hata!',
    warning: 'Uyarı!',
    info: 'Bilgi'
};

// ─── Varsayılan Süreler (ms) ───
const TOAST_DURATIONS = {
    success: 4000,
    error: 6000,   // Hatalar daha uzun süre görünsün
    warning: 5000,
    info: 4000
};


/**
 * Tek bir toast bildirimi oluşturur ve gösterir
 * @param {string} type - 'success' | 'error' | 'warning' | 'info'
 * @param {string} message - Gösterilecek mesaj
 * @param {string} [title] - Başlık (opsiyonel, varsayılan tip başlığı)
 * @param {number} [duration] - Gösterim süresi ms (opsiyonel)
 */
function showToast(type, message, title, duration) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    // Varsayılan değerleri ata
    title = title || TOAST_TITLES[type] || 'Bildirim';
    duration = duration || TOAST_DURATIONS[type] || 4000;
    const icon = TOAST_ICONS[type] || 'fa-bell';

    // Toast HTML oluştur
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <div class="toast-icon">
            <i class="fas ${icon}"></i>
        </div>
        <div class="toast-content">
            <p class="toast-title">${escapeHtml(title)}</p>
            <p class="toast-message">${escapeHtml(message)}</p>
        </div>
        <button class="toast-close" title="Kapat">
            <i class="fas fa-times"></i>
        </button>
        <div class="toast-progress" style="animation-duration: ${duration}ms;"></div>
    `;

    // Kapatma butonu
    const closeBtn = toast.querySelector('.toast-close');
    closeBtn.addEventListener('click', () => removeToast(toast));

    // Container'a ekle
    container.appendChild(toast);

    // Maksimum 5 toast göster, eskilerini kaldır
    const allToasts = container.querySelectorAll('.toast');
    if (allToasts.length > 5) {
        removeToast(allToasts[0]);
    }

    // Otomatik kaldır (süre bitince)
    setTimeout(() => removeToast(toast), duration);
}


/**
 * Toast'ı animasyonlu şekilde kaldırır
 */
function removeToast(toastElement) {
    if (!toastElement || toastElement.classList.contains('toast-exit')) return;

    toastElement.classList.add('toast-exit');

    // Animasyon bitince DOM'dan kaldır
    toastElement.addEventListener('animationend', () => {
        toastElement.remove();
    });
}


/**
 * XSS koruması - HTML karakterlerini escape et
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}


// ─── GLOBAL TOAST API ───
// Diğer JS dosyalarından kolayca erişilebilir
const Toast = {
    success(message, title, duration) {
        showToast('success', message, title, duration);
    },

    error(message, title, duration) {
        showToast('error', message, title, duration);
    },

    warning(message, title, duration) {
        showToast('warning', message, title, duration);
    },

    info(message, title, duration) {
        showToast('info', message, title, duration);
    }
};


// ─── API FETCH WRAPPER ───
// Bu fonksiyon fetch çağrılarını sarar ve otomatik toast gösterir.
// Diğer JS dosyalarında (app.js, face-lock-panel.js vb.) kullanılabilir.
/**
 * @param {string} url - API endpoint
 * @param {object} options - fetch options + ek ayarlar
 * @param {string} [options.successMessage] - Başarıda gösterilecek toast mesajı
 * @param {string} [options.errorMessage] - Hatada gösterilecek toast mesajı (varsayılan var)
 * @param {boolean} [options.showSuccessToast=false] - Başarı toast'ı gösterilsin mi?
 * @returns {Promise<object|null>} - API yanıtı veya null (hata durumunda)
 * 
 * ÖRNEK KULLANIM:
 * 
 *  // GET isteği
 *  const data = await apiFetch('/RenderNet/GetFluxStyles');
 * 
 *  // POST isteği (JSON)
 *  const result = await apiFetch('/RenderNet/GenerateImage', {
 *      method: 'POST',
 *      headers: { 'Content-Type': 'application/json' },
 *      body: JSON.stringify(payload),
 *      successMessage: 'Görsel oluşturuluyor!',
 *      showSuccessToast: true
 *  });
 * 
 *  // POST isteği (FormData - dosya yükleme)
 *  const formData = new FormData();
 *  formData.append('formFile', file);
 *  const uploadResult = await apiFetch('/RenderNet/GetAssetId', {
 *      method: 'POST',
 *      body: formData,
 *      successMessage: 'Dosya yüklendi!',
 *      showSuccessToast: true
 *  });
 */
async function apiFetch(url, options = {}) {
    // Ek ayarları çıkar (fetch'e gitmemeli)
    const {
        successMessage,
        errorMessage,
        showSuccessToast = false,
        headers: customHeaders = {},
        ...fetchOptions
    } = options;

    fetchOptions.headers = {
        ...customHeaders
    };

    try {
        const response = await fetch(url, fetchOptions);

        // HTTP durum kodu kontrolü
        if (!response.ok) {
            // Sunucu JSON hata mesajı döndüyse oku
            let serverMessage = errorMessage || 'İşlem sırasında bir hata oluştu.';
            try {
                const errorData = await response.json();
                serverMessage = errorData.message || serverMessage;
            } catch {
                // JSON parse edilemezse varsayılan mesaj
            }

            Toast.error(serverMessage);
            console.error(`[API Error] ${response.status} - ${url}:`, serverMessage);
            return null;
        }

        // Başarılı yanıtı parse et
        const data = await response.json();

        // Sunucu success: false döndüyse (business logic hatası)
        if (data.success === false) {
            Toast.error(data.message || errorMessage || 'İşlem başarısız.');
            return null;
        }

        // Başarı toast'ı göster (isteniyorsa)
        if (showSuccessToast && successMessage) {
            Toast.success(successMessage);
        }

        return data;

    } catch (error) {
        // Ağ hatası, timeout vb.
        console.error(`[Network Error] ${url}:`, error);
        Toast.error(
            errorMessage || 'Sunucu ile bağlantı kurulamadı. İnternet bağlantınızı kontrol edin.',
            'Bağlantı Hatası'
        );
        return null;
    }
}


// ─── Global erişim için window'a ekle ───
window.Toast = Toast;
window.apiFetch = apiFetch;