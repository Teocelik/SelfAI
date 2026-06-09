/* ============================================================
   landing.js — F.0 Landing Page etkileşimleri
   Mevcut IIFE modül kalıbı (CLAUDE.md). Bağımlılık yok, framework yok.
   Tamamen progressive enhancement: JS yoksa sayfa eksiksiz çalışır,
   sadece reveal/tilt efektleri devre dışı kalır.
   ============================================================ */
const Landing = (function () {
    'use strict';

    // Klavye/erişilebilirlik: kullanıcı animasyon istemiyorsa hiçbir efekt kurma.
    const prefersReducedMotion = window.matchMedia
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    /**
     * Scroll'da bölümleri/kartları yumuşakça belirir (reveal on scroll).
     * IntersectionObserver desteklenmiyorsa öğeler doğal görünür kalır
     * (CSS .landing-reveal başlangıç class'ı yalnızca burada eklenir).
     */
    function initScrollReveal() {
        if (!('IntersectionObserver' in window)) {
            return;
        }

        const targets = document.querySelectorAll(
            '.showcase-card, .feature-card, .landing-features__title'
        );
        if (targets.length === 0) {
            return;
        }

        const observer = new IntersectionObserver(function (entries, obs) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    obs.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15, rootMargin: '0px 0px -40px 0px' });

        targets.forEach(function (el, index) {
            // Başlangıç gizli durumunu yalnızca JS aktifken uygula (no-JS = görünür).
            el.classList.add('landing-reveal');
            // Hafif kademeli (stagger) gecikme — kartlar sırayla belirir.
            el.style.transitionDelay = Math.min(index % 6, 5) * 60 + 'ms';
            observer.observe(el);
        });
    }

    /**
     * Showcase kartlarına imleç takipli hafif 3B tilt — "alive" cam hissi.
     * Yalnızca pointer (mouse) ortamlarında, reduced-motion kapalıyken çalışır.
     */
    function initTilt() {
        if (prefersReducedMotion) {
            return;
        }
        if (!window.matchMedia || !window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
            return; // dokunmatik cihazlarda tilt yok
        }

        const cards = document.querySelectorAll('.showcase-card');
        const MAX_TILT = 6; // derece

        cards.forEach(function (card) {
            card.addEventListener('mousemove', function (e) {
                const rect = card.getBoundingClientRect();
                const px = (e.clientX - rect.left) / rect.width;  // 0..1
                const py = (e.clientY - rect.top) / rect.height;  // 0..1
                const rotateY = (px - 0.5) * 2 * MAX_TILT;
                const rotateX = (0.5 - py) * 2 * MAX_TILT;
                card.style.transform =
                    'perspective(700px) rotateX(' + rotateX.toFixed(2) + 'deg) ' +
                    'rotateY(' + rotateY.toFixed(2) + 'deg) scale(1.05)';
            });

            card.addEventListener('mouseleave', function () {
                // CSS hover/transition'ı geri devralsın diye inline transform'u temizle.
                card.style.transform = '';
            });
        });
    }

    function init() {
        initScrollReveal();
        initTilt();
    }

    return { init };
})();

document.addEventListener('DOMContentLoaded', Landing.init);
