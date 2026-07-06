/* ============================================================
   landing.js — F.7.2 Landing Page etkileşimleri
   IIFE modül kalıbı (CLAUDE.md). Bağımlılık yok, framework yok.
   Progressive enhancement: JS yoksa sayfa çalışır, ilk slide görünür kalır.
   ============================================================ */
const Landing = (function () {
    'use strict';

    const SLIDE_INTERVAL_MS = 4000;

    let slides = [];
    let indicators = [];
    let currentIndex = 0;
    let intervalId = null;

    function init() {
        initHeroSlider();
        initSmoothScroll();
    }

    function initHeroSlider() {
        const slider = document.getElementById('heroSlider');
        const indicatorsEl = document.getElementById('heroIndicators');
        if (!slider || !indicatorsEl) return;

        slides = Array.from(slider.querySelectorAll('.hero__slide'));
        indicators = Array.from(indicatorsEl.querySelectorAll('.hero__indicator'));

        if (slides.length <= 1) return;

        indicators.forEach((btn, idx) => {
            btn.addEventListener('click', () => {
                goToSlide(idx);
                startAutoRotate();
            });
        });

        startAutoRotate();

        document.addEventListener('visibilitychange', function () {
            if (document.hidden) stopAutoRotate();
            else startAutoRotate();
        });
    }

    function goToSlide(index) {
        if (index === currentIndex || index < 0 || index >= slides.length) return;

        slides[currentIndex].classList.remove('hero__slide--active');
        indicators[currentIndex].classList.remove('hero__indicator--active');

        currentIndex = index;

        slides[currentIndex].classList.add('hero__slide--active');
        indicators[currentIndex].classList.add('hero__indicator--active');
    }

    function nextSlide() {
        goToSlide((currentIndex + 1) % slides.length);
    }

    function startAutoRotate() {
        stopAutoRotate();
        intervalId = setInterval(nextSlide, SLIDE_INTERVAL_MS);
    }

    function stopAutoRotate() {
        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }
    }

    function initSmoothScroll() {
        const anchorLinks = document.querySelectorAll('a[href^="#"]');
        anchorLinks.forEach(link => {
            link.addEventListener('click', function (e) {
                const href = this.getAttribute('href');
                if (!href || href === '#') return;

                const target = document.querySelector(href);
                if (!target) return;

                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            });
        });
    }

    return { init };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', Landing.init);
} else {
    Landing.init();
}
