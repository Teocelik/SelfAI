const CookieBanner = (function () {
    'use strict';

    const STORAGE_KEY = 'selfai_cookie_consent';
    let bannerEl, acceptBtnEl;

    function init() {
        bannerEl = document.getElementById('cookieBanner');
        acceptBtnEl = document.getElementById('cookieAcceptBtn');

        if (!bannerEl) return;

        const consent = localStorage.getItem(STORAGE_KEY);
        if (consent === 'accepted') {
            bannerEl.hidden = true;
            return;
        }

        bannerEl.hidden = false;

        if (acceptBtnEl) {
            acceptBtnEl.addEventListener('click', accept);
        }
    }

    function accept() {
        localStorage.setItem(STORAGE_KEY, 'accepted');
        bannerEl.hidden = true;
    }

    return { init };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', CookieBanner.init);
} else {
    CookieBanner.init();
}
