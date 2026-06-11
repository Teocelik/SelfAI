/**
 * Lightbox Module (F.7)
 * Output gallery görsellerine tam etkileşim: büyütme + indir + paylaş.
 *
 * IIFE pattern (mevcut konvansiyon). Event delegation ile ImageControls'un
 * dinamik olarak eklediği output kartlarını yakalar. _AppLayout seviyesinde
 * yüklenir — ileride History (F.3) sayfasında da kullanılacak.
 */
(function () {
    'use strict';

    let lightbox = null;
    let currentImages = [];
    let currentIndex = 0;
    let lastFocusedElement = null;

    document.addEventListener('DOMContentLoaded', function () {
        createLightboxDOM();
        attachOutputGalleryListeners();
        attachKeyboardListeners();
    });

    function createLightboxDOM() {
        lightbox = document.createElement('div');
        lightbox.className = 'lightbox';
        lightbox.setAttribute('role', 'dialog');
        lightbox.setAttribute('aria-modal', 'true');
        lightbox.setAttribute('aria-label', 'Görsel önizleme');

        lightbox.innerHTML = `
            <div class="lightbox__container">
                <button type="button" class="lightbox__close" aria-label="Kapat">
                    <i class="fas fa-times"></i>
                </button>

                <div class="lightbox__image-wrapper">
                    <button type="button" class="lightbox__nav lightbox__nav--prev" aria-label="Önceki görsel">
                        <i class="fas fa-chevron-left"></i>
                    </button>
                    <img class="lightbox__image" alt="" />
                    <button type="button" class="lightbox__nav lightbox__nav--next" aria-label="Sonraki görsel">
                        <i class="fas fa-chevron-right"></i>
                    </button>
                </div>

                <div class="lightbox__actions">
                    <span class="lightbox__counter"></span>
                    <a class="lightbox__action lightbox__download" download target="_blank" rel="noopener">
                        <i class="fas fa-download"></i>
                        <span>İndir</span>
                    </a>
                    <button type="button" class="lightbox__action lightbox__share">
                        <i class="fas fa-share-alt"></i>
                        <span>Paylaş</span>
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(lightbox);

        // Close handlers
        lightbox.querySelector('.lightbox__close').addEventListener('click', closeLightbox);
        lightbox.addEventListener('click', function (e) {
            if (e.target === lightbox) closeLightbox();  // outside click
        });

        // Navigation handlers
        lightbox.querySelector('.lightbox__nav--prev').addEventListener('click', showPrevImage);
        lightbox.querySelector('.lightbox__nav--next').addEventListener('click', showNextImage);

        // Share handler
        lightbox.querySelector('.lightbox__share').addEventListener('click', shareCurrentImage);
    }

    function attachOutputGalleryListeners() {
        // Event delegation — output gallery container'ı bulup oradan dinle
        // ImageControls.showGeneratedImages dinamik DOM ekliyor, statik bind yetmez
        document.body.addEventListener('click', function (e) {
            // History card pattern (D.4 + F.3) — kart üzerindeki tüm görseller JSON dataset'te
            const historyCard = e.target.closest('[data-history-card]');
            if (historyCard && historyCard.dataset.mediaUrls) {
                try {
                    const urls = JSON.parse(historyCard.dataset.mediaUrls);
                    if (Array.isArray(urls) && urls.length > 0) {
                        e.preventDefault();
                        openLightbox(urls, 0);
                    }
                } catch (err) {
                    console.error('History card media URL parse hatası:', err);
                }
                return;
            }

            // Mevcut Studio output gallery pattern (F.7)
            const card = e.target.closest('[data-output-card], .output-card');
            if (!card) return;

            // Eğer karta inline button (download, regenerate vb.) basıldıysa lightbox açma
            if (e.target.closest('button, a')) return;

            // Generation'ın tüm görsellerini topla
            const galleryContainer = card.closest('[data-output-gallery], .output-gallery');
            if (!galleryContainer) return;

            const imgs = galleryContainer.querySelectorAll('[data-output-card] img, .output-card img');
            const urls = Array.from(imgs).map(function (img) { return img.src; });
            const clickedIndex = Array.from(imgs).findIndex(function (img) {
                return img.src === card.querySelector('img').src;
            });

            if (urls.length > 0) {
                openLightbox(urls, Math.max(0, clickedIndex));
            }
        });
    }

    function attachKeyboardListeners() {
        document.addEventListener('keydown', function (e) {
            if (!lightbox.classList.contains('is-open')) return;

            switch (e.key) {
                case 'Escape':
                    closeLightbox();
                    break;
                case 'ArrowLeft':
                    showPrevImage();
                    break;
                case 'ArrowRight':
                    showNextImage();
                    break;
            }
        });
    }

    function openLightbox(urls, index) {
        currentImages = urls;
        currentIndex = index || 0;
        lastFocusedElement = document.activeElement;

        updateLightboxImage();
        lightbox.classList.add('is-open');
        document.body.style.overflow = 'hidden';  // background scroll lock

        // Focus to close button (basic focus trap entry)
        setTimeout(function () {
            lightbox.querySelector('.lightbox__close').focus();
        }, 100);
    }

    function closeLightbox() {
        lightbox.classList.remove('is-open');
        document.body.style.overflow = '';

        // Focus return
        if (lastFocusedElement && lastFocusedElement.focus) {
            lastFocusedElement.focus();
        }
    }

    function showPrevImage() {
        if (currentIndex > 0) {
            currentIndex--;
            updateLightboxImage();
        }
    }

    function showNextImage() {
        if (currentIndex < currentImages.length - 1) {
            currentIndex++;
            updateLightboxImage();
        }
    }

    function updateLightboxImage() {
        const img = lightbox.querySelector('.lightbox__image');
        const downloadLink = lightbox.querySelector('.lightbox__download');
        const counter = lightbox.querySelector('.lightbox__counter');
        const prevBtn = lightbox.querySelector('.lightbox__nav--prev');
        const nextBtn = lightbox.querySelector('.lightbox__nav--next');

        const url = currentImages[currentIndex];
        img.classList.add('loading');
        img.src = url;
        img.onload = function () { img.classList.remove('loading'); };

        downloadLink.href = url;
        downloadLink.download = `selfai-generation-${Date.now()}-${currentIndex + 1}.png`;

        if (currentImages.length > 1) {
            counter.textContent = `${currentIndex + 1} / ${currentImages.length}`;
            counter.style.display = '';
            prevBtn.style.display = '';
            nextBtn.style.display = '';
            prevBtn.disabled = (currentIndex === 0);
            nextBtn.disabled = (currentIndex === currentImages.length - 1);
        } else {
            counter.style.display = 'none';
            prevBtn.style.display = 'none';
            nextBtn.style.display = 'none';
        }
    }

    async function shareCurrentImage() {
        const url = currentImages[currentIndex];

        // Modern share API (mobile + Safari)
        if (navigator.share) {
            try {
                await navigator.share({
                    title: 'SelfAI ile üretilen görsel',
                    text: 'SelfAI ile bu görseli ürettim',
                    url: url
                });
                return;
            } catch (err) {
                // User cancelled or failed — fallback to clipboard
                if (err.name === 'AbortError') return;
            }
        }

        // Fallback: clipboard
        try {
            await navigator.clipboard.writeText(url);
            if (typeof Toast !== 'undefined' && Toast.success) {
                Toast.success('Görsel bağlantısı kopyalandı!');
            } else {
                alert('Görsel bağlantısı panoya kopyalandı.');
            }
        } catch (err) {
            console.error('Paylaşım hatası:', err);
            if (typeof Toast !== 'undefined' && Toast.error) {
                Toast.error('Paylaşım yapılamadı.');
            }
        }
    }
})();
