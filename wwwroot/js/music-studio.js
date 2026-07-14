/**
 * MusicStudio (F.M.10c) — Studio/Music sekmesi.
 *
 * Mimari: template-studio.js ile aynı — app.js bu sayfada YÜKLENMEZ, bu yüzden kendi
 * SignalR bağlantımızı kurarız. Üretim fire-and-forget'tir; sonuç senkron fetch
 * response'unda DEĞİL, SignalR "MusicGenerationUpdate" / "MusicGenerationFailed"
 * event'iyle gelir. Yalnızca bu sayfadan başlatılan generationId'ler işlenir.
 *
 * Bağımlılıklar (global): signalR (CDN, _AppLayout), Toast (toast.js).
 */
const MusicStudio = (function () {
    'use strict';

    const BACKING_DURATIONS = [15, 30, 60];
    const SONG_DURATIONS = [60, 90, 180];

    let currentMode = 'backing';
    let selectedDuration = null;
    let selectedAspect = '1:1';
    let isGenerating = false;
    let signalRConnection = null;

    // Bekleyen üretimler: generationId (string) → true. Yalnızca bu sayfanın ID'leri.
    const pendingGenerationIds = new Set();

    let formEl, modeButtonEls, promptEl, promptCharEl, lyricsFieldEl, lyricsEl,
        lyricsCharEl, durationOptionsEl, durationNoteEl, coverOptionEls,
        generateBtnEl, resultsWrapEl, resultsListEl;

    function init() {
        formEl = document.getElementById('musicForm');
        modeButtonEls = document.querySelectorAll('.mode-toggle-option');
        promptEl = document.getElementById('musicPromptInput');
        promptCharEl = document.getElementById('promptCharCount');
        lyricsFieldEl = document.getElementById('lyricsField');
        lyricsEl = document.getElementById('lyricsInput');
        lyricsCharEl = document.getElementById('lyricsCharCount');
        durationOptionsEl = document.getElementById('durationOptions');
        durationNoteEl = document.getElementById('durationNote');
        coverOptionEls = document.querySelectorAll('.cover-option');
        generateBtnEl = document.getElementById('musicGenerateBtn');
        resultsWrapEl = document.getElementById('musicResults');
        resultsListEl = document.getElementById('musicResultsList');

        if (!formEl) return;

        // Inline handler YASAK (CLAUDE.md) — submit'i burada engelle.
        formEl.addEventListener('submit', function (e) { e.preventDefault(); });

        modeButtonEls.forEach(function (btn) {
            btn.addEventListener('click', function () { switchMode(btn.dataset.mode); });
        });

        promptEl.addEventListener('input', function () {
            promptCharEl.textContent = String(promptEl.value.length);
            updateGenerateButton();
        });

        lyricsEl.addEventListener('input', function () {
            lyricsCharEl.textContent = String(lyricsEl.value.length);
        });

        coverOptionEls.forEach(function (opt) {
            opt.addEventListener('click', function () { selectCover(opt); });
        });

        generateBtnEl.addEventListener('click', onGenerate);

        renderDurations();
        initSignalR();
    }

    // ═══════════════════════════════════════════════
    // SIGNALR — kendi bağlantımız
    // ═══════════════════════════════════════════════

    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('[MusicStudio] SignalR library not loaded');
            return;
        }

        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl('/generationHub')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        signalRConnection.on('MusicGenerationUpdate', onMusicUpdate);
        signalRConnection.on('MusicGenerationFailed', onMusicFailed);

        startSignalRConnection();
    }

    async function startSignalRConnection() {
        try {
            await signalRConnection.start();
            console.log('[MusicStudio][SignalR] Bağlandı. ConnectionId:', signalRConnection.connectionId);
        } catch (err) {
            console.error('[MusicStudio][SignalR] Bağlantı hatası:', err);
            setTimeout(startSignalRConnection, 5000);
        }
    }

    function getConnectionId() {
        return signalRConnection && signalRConnection.connectionId ? signalRConnection.connectionId : null;
    }

    function onMusicUpdate(result) {
        if (!result || !result.generationId) return;
        if (!pendingGenerationIds.has(result.generationId)) return;

        pendingGenerationIds.delete(result.generationId);
        addResultCard(result);
        resetGeneratingState();
        showToast('success', 'Müzik hazır!');
        refreshCreditBalance();
    }

    function onMusicFailed(fail) {
        if (!fail || !fail.generationId) return;
        if (!pendingGenerationIds.has(fail.generationId)) return;

        pendingGenerationIds.delete(fail.generationId);
        resetGeneratingState();
        showToast('error', fail.message || 'Müzik üretim başarısız oldu, krediniz iade edildi.');
        refreshCreditBalance();
    }

    // ═══════════════════════════════════════════════
    // FORM ETKİLEŞİMLERİ
    // ═══════════════════════════════════════════════

    function switchMode(mode) {
        currentMode = mode;
        modeButtonEls.forEach(function (btn) {
            btn.classList.toggle('is-active', btn.dataset.mode === mode);
        });

        lyricsFieldEl.hidden = mode !== 'song';
        durationNoteEl.hidden = mode !== 'song';
        selectedDuration = null;
        renderDurations();
        updateGenerateButton();
    }

    function renderDurations() {
        const durations = currentMode === 'backing' ? BACKING_DURATIONS : SONG_DURATIONS;
        durationOptionsEl.innerHTML = '';

        durations.forEach(function (sec) {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'duration-option';
            btn.dataset.duration = String(sec);
            btn.textContent = sec < 60 ? (sec + ' sn') : ((sec / 60) + ' dk');
            btn.addEventListener('click', function () { selectDuration(sec); });
            durationOptionsEl.appendChild(btn);
        });
    }

    function selectDuration(sec) {
        selectedDuration = sec;
        durationOptionsEl.querySelectorAll('.duration-option').forEach(function (b) {
            b.classList.toggle('is-selected', Number(b.dataset.duration) === sec);
        });
        updateGenerateButton();
    }

    function selectCover(el) {
        selectedAspect = el.dataset.aspect;
        coverOptionEls.forEach(function (o) { o.classList.toggle('is-selected', o === el); });
    }

    function updateGenerateButton() {
        const promptOk = promptEl.value.trim().length >= 10;
        const durationOk = selectedDuration !== null;
        generateBtnEl.disabled = isGenerating || !promptOk || !durationOk;
    }

    // ═══════════════════════════════════════════════
    // ÜRETİM
    // ═══════════════════════════════════════════════

    async function onGenerate() {
        if (isGenerating) return;

        const prompt = promptEl.value.trim();
        if (prompt.length < 10 || selectedDuration === null) return;

        const connectionId = getConnectionId();
        if (!connectionId) {
            showToast('error', 'Sunucu ile bağlantı kurulamadı. Sayfayı yenileyin.');
            return;
        }

        isGenerating = true;
        setGeneratingState(true);

        const payload = {
            mode: currentMode,
            musicPrompt: prompt,
            lyrics: currentMode === 'song' ? (lyricsEl.value.trim() || null) : null,
            durationSeconds: selectedDuration,
            coverAspectRatio: selectedAspect
        };

        try {
            const response = await fetch('/Studio/Music/Generate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-SignalR-ConnectionId': connectionId
                },
                body: JSON.stringify(payload),
                credentials: 'include'
            });

            const data = await response.json().catch(function () { return {}; });

            if (!response.ok) {
                showToast('error', data.message || 'Müzik üretim başlatılamadı.');
                resetGeneratingState();
                return;
            }

            if (data.generationId) {
                pendingGenerationIds.add(data.generationId);
            }
            showToast('info', 'Müzik üretiliyor... 1-3 dakika sürebilir.');
            refreshCreditBalance();
            // isGenerating SignalR sonucunda (update/failed) temizlenir.
        } catch (err) {
            console.error('[MusicStudio] Generate hatası:', err);
            showToast('error', 'Bir hata oluştu.');
            resetGeneratingState();
        }
    }

    function setGeneratingState(active) {
        const label = generateBtnEl.querySelector('.music-form__submit-label');
        const spinner = generateBtnEl.querySelector('.music-form__submit-spinner');
        if (label) label.textContent = active ? 'Üretiliyor...' : 'Üret';
        if (spinner) spinner.hidden = !active;
        updateGenerateButton();
    }

    function resetGeneratingState() {
        isGenerating = false;
        setGeneratingState(false);
    }

    // ═══════════════════════════════════════════════
    // SONUÇ KARTLARI
    // ═══════════════════════════════════════════════

    function addResultCard(result) {
        resultsWrapEl.hidden = false;

        const template = document.getElementById('musicResultTemplate');
        const clone = template.content.cloneNode(true);

        const coverWrap = clone.querySelector('.music-result-card__cover');
        const coverImg = clone.querySelector('.music-result-card__cover-image');
        const coverDownload = clone.querySelector('.music-result-card__download-cover');

        if (result.coverUrl) {
            coverImg.src = result.coverUrl;
            coverImg.hidden = false;
            coverDownload.href = result.coverUrl;
            coverDownload.hidden = false;
        } else {
            // Kapak üretilemedi — gradient fallback (placeholder asset yok).
            coverWrap.classList.add('music-result-card__cover--empty');
        }

        clone.querySelector('.music-result-card__mode').textContent =
            result.mode === 'backing' ? 'Backing Track' : 'Full Song';
        clone.querySelector('.music-result-card__prompt').textContent = result.musicPrompt || '';

        const audioEl = clone.querySelector('.music-result-card__audio');
        audioEl.src = result.audioUrl;

        clone.querySelector('.music-result-card__download').href = result.audioUrl;

        resultsListEl.prepend(clone);
        resultsWrapEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    // ═══════════════════════════════════════════════
    // YARDIMCILAR
    // ═══════════════════════════════════════════════

    // Kredi bakiyesi göstergesi (nav pill). app.js burada yok, bu yüzden yerel refresh.
    async function refreshCreditBalance() {
        const valueEl = document.getElementById('creditBalanceValue');
        if (!valueEl) return;
        try {
            const response = await fetch('/Account/Balance');
            if (response.ok) {
                const data = await response.json();
                if (data.success && data.balance !== undefined) {
                    valueEl.textContent = data.balance;
                }
            }
        } catch (err) {
            console.error('[MusicStudio] Balance refresh hatası:', err);
        }
    }

    function showToast(type, message) {
        if (window.Toast && typeof window.Toast[type] === 'function') {
            window.Toast[type](message);
        }
    }

    return { init };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', MusicStudio.init);
} else {
    MusicStudio.init();
}
