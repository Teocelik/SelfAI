/**
 * History Music Player (F.M.UI.3)
 * /History müzik kartlarına tıklayınca Spotify tarzı modal açar:
 * büyük kapak + HTML5 audio + indir butonları.
 *
 * IIFE pattern (mevcut konvansiyon). Müzik kartları [data-history-music-card]
 * kullanır; lightbox.js yalnızca [data-history-card]'ı yakaladığından çakışma yoktur.
 */
const HistoryMusicPlayer = (function () {
    'use strict';

    let modalEl, audioEl, coverEl, titleEl, dateEl, downloadEl, coverDownloadEl;

    function init() {
        modalEl = document.getElementById('musicPlayerModal');
        if (!modalEl) return;

        audioEl = document.getElementById('playerAudio');
        coverEl = document.getElementById('playerCoverImage');
        titleEl = document.getElementById('playerTitle');
        dateEl = document.getElementById('playerDate');
        downloadEl = document.getElementById('playerDownload');
        coverDownloadEl = document.getElementById('playerCoverDownload');

        // Müzik kart tıklama + klavye erişimi
        document.querySelectorAll('[data-history-music-card]').forEach(function (card) {
            card.addEventListener('click', function () { openPlayer(card); });
            card.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    openPlayer(card);
                }
            });
        });

        // Kapat butonları (backdrop + X)
        modalEl.querySelectorAll('[data-modal-close]').forEach(function (el) {
            el.addEventListener('click', closePlayer);
        });

        // ESC ile kapat
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !modalEl.hidden) {
                closePlayer();
            }
        });
    }

    function openPlayer(card) {
        const audioUrl = card.dataset.audioUrl || '';
        const coverUrl = card.dataset.coverUrl || '';
        const prompt = card.dataset.prompt || 'Müzik üretimi';
        const date = card.dataset.date || '';

        if (!audioUrl) {
            if (typeof Toast !== 'undefined' && Toast.error) {
                Toast.error('Bu üretim için ses bağlantısı bulunamadı.');
            }
            return;
        }

        audioEl.src = audioUrl;
        titleEl.textContent = prompt;
        dateEl.textContent = date;
        downloadEl.href = audioUrl;

        // Kapak varsa göster + indir butonu; yoksa CSS gradient fallback + indir gizli
        if (coverUrl) {
            coverEl.src = coverUrl;
            coverEl.style.visibility = '';
            coverDownloadEl.href = coverUrl;
            coverDownloadEl.hidden = false;
        } else {
            coverEl.removeAttribute('src');
            coverEl.style.visibility = 'hidden';
            coverDownloadEl.removeAttribute('href');
            coverDownloadEl.hidden = true;
        }

        modalEl.hidden = false;
        modalEl.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';

        // Auto-play (bazı tarayıcılar engeller — sessiz fail)
        audioEl.play().catch(function () { /* auto-play blocked */ });
    }

    function closePlayer() {
        audioEl.pause();
        audioEl.removeAttribute('src');
        audioEl.load();
        modalEl.hidden = true;
        modalEl.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
    }

    return { init };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', HistoryMusicPlayer.init);
} else {
    HistoryMusicPlayer.init();
}
