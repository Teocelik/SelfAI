/* ============================================================
   login.js — F.2 Dedicated Login glue code
   ------------------------------------------------------------
   ÖNEMLİ: Auth logic'i DUPLICATE ETMEZ.
   - Google girişi  → firebase-auth-google.js (#googleLoginBtn click'ine kendi bağlanır)
   - E-posta linki   → firebase-auth-link.js   (#emailLoginForm submit'ine kendi bağlanır)
   Her iki modül de returnUrl'i doğrudan window.location.search'ten okur; Login
   sayfası "/Account/Login?returnUrl=..." ile geldiği için bu otomatik çalışır.
   Bu yüzden login.js'te ikinci bir submit/click handler EKLENMEZ (çift VerifyToken
   / çift e-posta gönderimi olurdu). Buradaki tek sorumluluk: ortam doğrulama (guard)
   ve küçük UX iyileştirmeleri.
   ============================================================ */
const Login = (function () {
    'use strict';

    function init() {
        // 1) Firebase compat SDK + firebase-init.js yüklendi mi? (guard)
        // Yüklenmediyse auth modülleri sessizce patlar; kullanıcıyı dostça uyar.
        if (typeof window.firebase === 'undefined' || !window.firebaseAuth) {
            console.error('[Login] Firebase SDK yüklenemedi — auth modülleri çalışmayacak.');
            if (window.Toast) {
                Toast.error('Giriş servisi yüklenemedi. Lütfen sayfayı yenileyin.', 'Bağlantı Hatası');
            }
            return;
        }

        // 2) Küçük UX: e-posta alanına odaklan.
        const emailInput = document.getElementById('userEmail');
        if (emailInput) {
            emailInput.focus();
        }

        // 3) returnUrl: firebase-auth-*.js modülleri bunu window.location.search'ten
        // okuduğu için ekstra işlem gerekmez. Hidden #returnUrl input'u sunucu tarafı
        // doğrulama/ileride kullanım için tutulur (mevcut akış URL'i kullanır).
    }

    return { init };
})();

document.addEventListener('DOMContentLoaded', Login.init);
