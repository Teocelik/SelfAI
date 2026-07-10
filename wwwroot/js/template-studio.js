/**
 * TemplateStudio (F.M.10a) — Post Templates sekmesi.
 *
 * Mimari: mevcut Studio/Image generation pipeline'ı REUSE edilir. Üretim
 * fire-and-forget'tir; sonuç senkron fetch response'unda DEĞİL, SignalR
 * "GenerationUpdate" event'iyle gelir (app.js ile birebir aynı pattern).
 *
 * Bağımlılıklar (global): signalR (CDN), Toast + apiFetch (toast.js). app.js
 * bu sayfada YÜKLENMEZ, bu yüzden kendi SignalR bağlantımızı kurarız.
 */
const TemplateStudio = (function () {
    'use strict';

    let selectedFormat = null;
    let signalRConnection = null;

    // Bekleyen üretimler: generationId (string) → { formatId, width, height }
    const pending = new Map();

    let formEl, promptEl, promptCharCountEl, usePromptSuffixEl,
        suffixPreviewEl, suffixCodeEl, generateBtnEl, formTitleEl,
        formSubtitleEl, resultsWrapEl, resultsGridEl;

    let isSubmitting = false;

    function init() {
        formEl = document.getElementById('templateForm');
        promptEl = document.getElementById('promptInput');
        promptCharCountEl = document.getElementById('promptCharCount');
        usePromptSuffixEl = document.getElementById('usePromptSuffix');
        suffixPreviewEl = document.getElementById('suffixPreview');
        suffixCodeEl = document.getElementById('suffixCode');
        generateBtnEl = document.getElementById('generateBtn');
        formTitleEl = document.getElementById('formTitle');
        formSubtitleEl = document.getElementById('formSubtitle');
        resultsWrapEl = document.getElementById('templateResults');
        resultsGridEl = document.getElementById('templateResultsGrid');

        document.querySelectorAll('.format-card').forEach(function (card) {
            card.addEventListener('click', function () { selectFormat(card); });
        });

        document.getElementById('formClose')?.addEventListener('click', closeForm);
        promptEl?.addEventListener('input', onPromptChange);
        usePromptSuffixEl?.addEventListener('change', updateSuffixPreview);
        generateBtnEl?.addEventListener('click', onGenerate);

        initSignalR();
    }

    // ═══════════════════════════════════════════════
    // SIGNALR — kendi bağlantımız (app.js bu sayfada yok)
    // ═══════════════════════════════════════════════

    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('[TemplateStudio] SignalR library not loaded');
            return;
        }

        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl('/generationHub')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // payload: { generationId, status:"Completed"|"Failed", images:[{url,width,height}], errorMessage }
        signalRConnection.on('GenerationUpdate', onGenerationUpdate);

        startSignalRConnection();
    }

    async function startSignalRConnection() {
        try {
            await signalRConnection.start();
            console.log('[TemplateStudio][SignalR] Bağlandı. ConnectionId:', signalRConnection.connectionId);
        } catch (err) {
            console.error('[TemplateStudio][SignalR] Bağlantı hatası:', err);
            setTimeout(startSignalRConnection, 5000);
        }
    }

    function getConnectionId() {
        return signalRConnection?.connectionId || null;
    }

    function onGenerationUpdate(payload) {
        if (!payload || !payload.generationId) return;

        // Yalnızca bu sayfadan başlatılan üretimlerle ilgilen (Studio/Image vb. yok say).
        const info = pending.get(payload.generationId);
        if (!info) return;
        pending.delete(payload.generationId);

        if (payload.status === 'Completed') {
            const url = (payload.images || [])
                .map(function (i) { return i && i.url; })
                .filter(Boolean)[0];

            if (url) {
                fillResultCard(payload.generationId, info, url);
                Toast.success('Görsel hazır!');
            } else {
                failResultCard(payload.generationId, 'Görsel URL\'i alınamadı.');
                Toast.error('Görsel URL\'i alınamadı.');
            }
        } else {
            // Failed — krediler backend tarafından iade edildi
            failResultCard(payload.generationId, payload.errorMessage || 'Üretim başarısız.');
            Toast.error(payload.errorMessage || 'Üretim başarısız oldu, krediniz iade edildi.');
            refreshCreditBalance();
        }
    }

    // ═══════════════════════════════════════════════
    // FORMAT SEÇİMİ
    // ═══════════════════════════════════════════════

    function selectFormat(cardEl) {
        document.querySelectorAll('.format-card').forEach(function (c) {
            c.classList.remove('is-selected');
        });
        cardEl.classList.add('is-selected');

        selectedFormat = {
            id: cardEl.dataset.formatId,
            name: cardEl.dataset.formatName,
            width: parseInt(cardEl.dataset.formatWidth, 10),
            height: parseInt(cardEl.dataset.formatHeight, 10),
            platform: cardEl.dataset.formatPlatform,
            aspect: cardEl.dataset.formatAspect,
            suffix: cardEl.dataset.formatSuffix
        };

        formTitleEl.textContent = selectedFormat.name;
        formSubtitleEl.textContent = selectedFormat.width + '×' + selectedFormat.height + ' • ' + selectedFormat.aspect;
        suffixCodeEl.textContent = selectedFormat.suffix;

        formEl.hidden = false;
        promptEl.value = '';
        promptCharCountEl.textContent = '0';
        usePromptSuffixEl.checked = true;
        updateSuffixPreview();
        updateGenerateButton();

        formEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
        promptEl.focus();
    }

    function closeForm() {
        formEl.hidden = true;
        selectedFormat = null;
        document.querySelectorAll('.format-card').forEach(function (c) {
            c.classList.remove('is-selected');
        });
    }

    function onPromptChange() {
        promptCharCountEl.textContent = String(promptEl.value.length);
        updateGenerateButton();
    }

    function updateSuffixPreview() {
        suffixPreviewEl.style.opacity = usePromptSuffixEl.checked ? '1' : '0.4';
    }

    function updateGenerateButton() {
        const promptLen = promptEl.value.trim().length;
        generateBtnEl.disabled = isSubmitting || !selectedFormat || promptLen < 3;
    }

    // ═══════════════════════════════════════════════
    // ÜRETİM
    // ═══════════════════════════════════════════════

    async function onGenerate() {
        if (!selectedFormat || isSubmitting) return;

        const prompt = promptEl.value.trim();
        if (prompt.length < 3) return;

        const connectionId = getConnectionId();
        if (!connectionId) {
            Toast.error('Sunucu ile bağlantı kurulamadı. Sayfayı yenileyin.', 'Bağlantı Hatası');
            return;
        }

        isSubmitting = true;
        setSubmittingState(true);

        const formatSnapshot = {
            formatId: selectedFormat.id,
            width: selectedFormat.width,
            height: selectedFormat.height
        };

        const result = await apiFetch('/Studio/Templates/Generate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-SignalR-ConnectionId': connectionId
            },
            body: JSON.stringify({
                formatId: selectedFormat.id,
                prompt: prompt,
                usePromptSuffix: usePromptSuffixEl.checked
            })
        });

        // apiFetch 401/402/hata durumlarında null döner + toast gösterir.
        if (result && result.data && result.data.generationId) {
            const generationId = result.data.generationId;
            pending.set(generationId, formatSnapshot);
            addPendingCard(generationId, formatSnapshot);
            Toast.info('Görsel oluşturuluyor, lütfen bekleyin...', 'İşleniyor');
            refreshCreditBalance();
        }

        // POST bittikten sonra buton tekrar aktif — kullanıcı yeni üretim başlatabilir.
        // Asıl bekleme her bir pending kartta gösterilir.
        isSubmitting = false;
        setSubmittingState(false);
    }

    function setSubmittingState(active) {
        const labelEl = generateBtnEl.querySelector('.template-form__submit-label');
        const spinnerEl = generateBtnEl.querySelector('.template-form__submit-spinner');
        if (labelEl) labelEl.textContent = active ? 'Gönderiliyor...' : 'Üret';
        if (spinnerEl) spinnerEl.hidden = !active;
        updateGenerateButton();
    }

    // ═══════════════════════════════════════════════
    // SONUÇ KARTLARI
    // ═══════════════════════════════════════════════

    function addPendingCard(generationId, info) {
        resultsWrapEl.hidden = false;

        const card = document.createElement('div');
        card.className = 'template-result-card template-result-card--pending';
        card.setAttribute('data-generation-id', generationId);
        card.innerHTML =
            '<div class="template-result-card__image-wrap">' +
                '<div class="template-result-card__pending">' +
                    '<div class="template-result-card__pending-spinner"></div>' +
                    '<span>Üretiliyor…</span>' +
                '</div>' +
            '</div>' +
            '<div class="template-result-card__meta">' +
                '<span class="template-result-card__format">' + escapeHtml(info.formatId) + ' • ' + info.width + '×' + info.height + '</span>' +
            '</div>';

        resultsGridEl.prepend(card);
        resultsWrapEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function fillResultCard(generationId, info, url) {
        const card = resultsGridEl.querySelector('[data-generation-id="' + cssEscape(generationId) + '"]');
        if (!card) return;

        card.classList.remove('template-result-card--pending');
        card.innerHTML =
            '<div class="template-result-card__image-wrap">' +
                '<img src="' + escapeHtml(url) + '" alt="Generated ' + escapeHtml(info.formatId) + '" ' +
                     'class="template-result-card__image" loading="lazy" />' +
            '</div>' +
            '<div class="template-result-card__meta">' +
                '<span class="template-result-card__format">' + escapeHtml(info.formatId) + ' • ' + info.width + '×' + info.height + '</span>' +
                '<a href="' + escapeHtml(url) + '" class="template-result-card__download" download target="_blank" rel="noopener">İndir</a>' +
            '</div>';
    }

    function failResultCard(generationId, message) {
        const card = resultsGridEl.querySelector('[data-generation-id="' + cssEscape(generationId) + '"]');
        if (!card) return;

        card.classList.remove('template-result-card--pending');
        card.classList.add('template-result-card--failed');
        card.innerHTML =
            '<div class="template-result-card__image-wrap">' +
                '<span>' + escapeHtml(message) + '</span>' +
            '</div>';
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
            console.error('[TemplateStudio] Balance refresh hatası:', err);
        }
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    // GUID'ler zaten güvenli, ama attribute selector'ında kullanmadan önce defansif escape.
    function cssEscape(str) {
        return String(str).replace(/["\\]/g, '\\$&');
    }

    return { init };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', TemplateStudio.init);
} else {
    TemplateStudio.init();
}
