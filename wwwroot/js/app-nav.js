// ═══════════════════════════════════════════════════════════
// App Top Navigation — mobile hamburger toggle (F.4.4)
// IIFE modül kalıbı (mevcut konvansiyon). Bağımsız çalışır.
// ═══════════════════════════════════════════════════════════
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const nav = document.querySelector('.app-nav');
        const hamburger = document.getElementById('appNavHamburger');

        if (!nav || !hamburger) return;

        // Hamburger ile menüyü aç/kapa
        hamburger.addEventListener('click', function (event) {
            event.stopPropagation();
            nav.classList.toggle('is-open');
        });

        // Mobile menüde bir link'e tıklanınca menüyü kapa
        const links = nav.querySelectorAll('.app-nav__link');
        links.forEach(function (link) {
            link.addEventListener('click', function () {
                nav.classList.remove('is-open');
            });
        });

        // Nav dışına tıklanınca menüyü kapa
        document.addEventListener('click', function (event) {
            if (!nav.contains(event.target)) {
                nav.classList.remove('is-open');
            }
        });
    });
})();
