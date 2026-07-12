/**
 * PresetStudio (F.M.10b) — Hazır Şablon sekmesi + paylaşımlı sekme/karakter kontrolü.
 *
 * Sorumluluk:
 *  - Sekme geçişi (Format Seç / Hazır Şablon) — paylaşımlı chrome.
 *  - Karakter seçici toggling — paylaşımlı chrome (template-studio.js DOM'dan okur).
 *  - Preset seçimi → dinamik text alanları + prompt → üretim.
 *  - Kendi SignalR bağlantısı: generation Complete olunca PostProcess ile text overlay
 *    render ettirir, final URL'i sonuç kartına yazar (template-studio.js'ten bağımsız,
 *    yalnızca kendi generationId'lerini işler).
 *
 * Bağımlılıklar (global): signalR (CDN), Toast + apiFetch (toast.js). template-studio.js
 * kendi SignalR bağlantısını ayrı kurar; iki modül birbirinin ID'lerini yok sayar.
 */
const PresetStudio = (function () {
    'use strict';

    let selectedPreset = null;   // { id, name, width, height, fields:[{id,label,placeholder,maxLength}] }
    let signalRConnection = null;
    let isSubmitting = false;

    // Bekleyen preset üretimleri: generationId → { presetId, width, height }
    const pending = new Map();

    let presetFormEl, presetPromptEl, presetTextFieldsEl, presetGenerateBtnEl,
        presetFormTitleEl, resultsWrapEl, resultsGridEl;

    function init() {
        presetFormEl = document.getElementById('presetForm');
        presetPromptEl = document.getElementById('presetPromptInput');
        presetTextFieldsEl = document.getElementById('presetTextFields');
        presetGenerateBtnEl = document.getElementById('presetGenerateBtn');
        presetFormTitleEl = document.getElementById('presetFormTitle');
        resultsWrapEl = document.getElementById('templateResults');
        resultsGridEl = document.getElementById('templateResultsGrid');

        // Sekme geçişi (paylaşımlı)
        document.querySelectorAll('.template-tab').forEach(function (tab) {
            tab.addEventListener('click', function () { switchTab(tab.dataset.tab); });
        });

        // Karakter seçici (paylaşımlı — bu modül toggle eder)
        document.querySelectorAll('.character-option').forEach(function (opt) {
            opt.addEventListener('click', function () { selectCharacter(opt); });
        });

        // Preset seçimi
        document.querySelectorAll('.preset-card').forEach(function (card) {
            card.addEventListener('click', function () { selectPreset(card); });
        });

        document.getElementById('presetFormClose')?.addEventListener('click', closePresetForm);
        presetPromptEl?.addEventListener('input', updatePresetGenerateButton);
        presetGenerateBtnEl?.addEventListener('click', onPresetGenerate);

        initSignalR();
    }

    // ═══════════════════════════════════════════════
    // SEKME + KARAKTER (paylaşımlı chrome)
    // ═══════════════════════════════════════════════

    function switchTab(tabName) {
        document.querySelectorAll('.template-tab').forEach(function (t) {
            t.classList.toggle('is-active', t.dataset.tab === tabName);
        });
        document.querySelectorAll('.template-tab-content').forEach(function (c) {
            c.classList.toggle('is-active', c.dataset.tabContent === tabName);
        });
    }

    function selectCharacter(optEl) {
        document.querySelectorAll('.character-option').forEach(function (o) {
            o.classList.remove('is-selected');
        });
        optEl.classList.add('is-selected');
    }

    function getSelectedCharacterId() {
        const opt = document.querySelector('.character-option.is-selected');
        const id = opt && opt.dataset ? opt.dataset.characterId : '';
        return id ? id : null;
    }

    // ═══════════════════════════════════════════════
    // SIGNALR — kendi bağlantımız
    // ═══════════════════════════════════════════════

    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('[PresetStudio] SignalR library not loaded');
            return;
        }

        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl('/generationHub')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        signalRConnection.on('GenerationUpdate', onGenerationUpdate);
        startSignalRConnection();
    }

    async function startSignalRConnection() {
        try {
            await signalRConnection.start();
            console.log('[PresetStudio][SignalR] Bağlandı. ConnectionId:', signalRConnection.connectionId);
        } catch (err) {
            console.error('[PresetStudio][SignalR] Bağlantı hatası:', err);
            setTimeout(startSignalRConnection, 5000);
        }
    }

    function getConnectionId() {
        return signalRConnection?.connectionId || null;
    }

    async function onGenerationUpdate(payload) {
        if (!payload || !payload.generationId) return;

        // Yalnızca bu modülün başlattığı preset üretimlerini işle.
        const info = pending.get(payload.generationId);
        if (!info) return;

        if (payload.status === 'Completed') {
            const sourceUrl = (payload.images || [])
                .map(function (i) { return i && i.url; })
                .filter(Boolean)[0];

            if (!sourceUrl) {
                pending.delete(payload.generationId);
                failResultCard(payload.generationId, 'Görsel URL\'i alınamadı.');
                Toast.error('Görsel URL\'i alınamadı.');
                return;
            }

            // Görsel hazır — şimdi text overlay için PostProcess çağır.
            await postProcessPreset(payload.generationId, info, sourceUrl);
        } else {
            // Failed — krediler backend tarafından iade edildi.
            pending.delete(payload.generationId);
            failResultCard(payload.generationId, payload.errorMessage || 'Üretim başarısız.');
            Toast.error(payload.errorMessage || 'Üretim başarısız oldu, krediniz iade edildi.');
            refreshCreditBalance();
        }
    }

    async function postProcessPreset(generationId, info, sourceUrl) {
        setCardStatusText(generationId, 'Metin ekleniyor…');

        const result = await apiFetch('/Studio/Templates/PostProcess', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ generationId: generationId, sourceImageUrl: sourceUrl })
        });

        pending.delete(generationId);

        if (result && result.data && result.data.imageUrl) {
            fillResultCard(generationId, info, result.data.imageUrl);
            Toast.success('Şablon hazır!');
        } else {
            // Overlay başarısız olsa da ham görsel elde var — onu göster, uyar.
            fillResultCard(generationId, info, sourceUrl);
            Toast.warning('Metin eklenemedi, düz görsel gösteriliyor.');
        }
    }

    // ═══════════════════════════════════════════════
    // PRESET SEÇİMİ + FORM
    // ═══════════════════════════════════════════════

    function selectPreset(cardEl) {
        document.querySelectorAll('.preset-card').forEach(function (c) {
            c.classList.remove('is-selected');
        });
        cardEl.classList.add('is-selected');

        let fields = [];
        try {
            fields = JSON.parse(cardEl.dataset.presetFields || '[]');
        } catch (err) {
            console.error('[PresetStudio] preset fields parse hatası:', err);
        }

        selectedPreset = {
            id: cardEl.dataset.presetId,
            name: cardEl.dataset.presetName,
            width: parseInt(cardEl.dataset.presetWidth, 10),
            height: parseInt(cardEl.dataset.presetHeight, 10),
            fields: fields
        };

        presetFormTitleEl.textContent = selectedPreset.name;
        presetPromptEl.value = '';
        renderTextFields(fields);

        presetFormEl.hidden = false;
        updatePresetGenerateButton();
        presetFormEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
        presetPromptEl.focus();
    }

    function renderTextFields(fields) {
        presetTextFieldsEl.innerHTML = '';
        fields.forEach(function (f) {
            const wrap = document.createElement('div');
            wrap.className = 'preset-form__field';

            const label = document.createElement('label');
            label.className = 'preset-form__label';
            label.setAttribute('for', 'presetField_' + f.id);
            label.textContent = f.label;

            const input = document.createElement('input');
            input.type = 'text';
            input.className = 'preset-form__input';
            input.id = 'presetField_' + f.id;
            input.setAttribute('data-field-id', f.id);
            input.placeholder = f.placeholder || '';
            if (f.maxLength) input.maxLength = f.maxLength;
            input.addEventListener('input', updatePresetGenerateButton);

            wrap.appendChild(label);
            wrap.appendChild(input);
            presetTextFieldsEl.appendChild(wrap);
        });
    }

    function closePresetForm() {
        presetFormEl.hidden = true;
        selectedPreset = null;
        document.querySelectorAll('.preset-card').forEach(function (c) {
            c.classList.remove('is-selected');
        });
    }

    function collectTextValues() {
        const values = {};
        presetTextFieldsEl.querySelectorAll('input[data-field-id]').forEach(function (input) {
            values[input.dataset.fieldId] = input.value.trim();
        });
        return values;
    }

    function updatePresetGenerateButton() {
        const promptLen = presetPromptEl.value.trim().length;
        presetGenerateBtnEl.disabled = isSubmitting || !selectedPreset || promptLen < 3;
    }

    // ═══════════════════════════════════════════════
    // ÜRETİM
    // ═══════════════════════════════════════════════

    async function onPresetGenerate() {
        if (!selectedPreset || isSubmitting) return;

        const prompt = presetPromptEl.value.trim();
        if (prompt.length < 3) return;

        const connectionId = getConnectionId();
        if (!connectionId) {
            Toast.error('Sunucu ile bağlantı kurulamadı. Sayfayı yenileyin.', 'Bağlantı Hatası');
            return;
        }

        isSubmitting = true;
        setSubmittingState(true);

        const snapshot = {
            presetId: selectedPreset.id,
            width: selectedPreset.width,
            height: selectedPreset.height
        };

        const result = await apiFetch('/Studio/Templates/Generate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-SignalR-ConnectionId': connectionId
            },
            body: JSON.stringify({
                presetId: selectedPreset.id,
                prompt: prompt,
                presetTextValues: collectTextValues(),
                characterId: getSelectedCharacterId()
            })
        });

        if (result && result.data && result.data.generationId) {
            const generationId = result.data.generationId;
            pending.set(generationId, snapshot);
            addPendingCard(generationId, snapshot);
            Toast.info('Görsel oluşturuluyor, ardından metin ekleniyor...', 'İşleniyor');
            refreshCreditBalance();
        }

        isSubmitting = false;
        setSubmittingState(false);
    }

    function setSubmittingState(active) {
        const labelEl = presetGenerateBtnEl.querySelector('.preset-form__submit-label');
        const spinnerEl = presetGenerateBtnEl.querySelector('.preset-form__submit-spinner');
        if (labelEl) labelEl.textContent = active ? 'Gönderiliyor...' : 'Üret';
        if (spinnerEl) spinnerEl.hidden = !active;
        updatePresetGenerateButton();
    }

    // ═══════════════════════════════════════════════
    // SONUÇ KARTLARI (paylaşımlı grid)
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
                    '<span class="template-result-card__status">Üretiliyor…</span>' +
                '</div>' +
            '</div>' +
            '<div class="template-result-card__meta">' +
                '<span class="template-result-card__format">' + escapeHtml(info.presetId) + ' • ' + info.width + '×' + info.height + '</span>' +
            '</div>';

        resultsGridEl.prepend(card);
        resultsWrapEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function setCardStatusText(generationId, text) {
        const card = resultsGridEl.querySelector('[data-generation-id="' + cssEscape(generationId) + '"]');
        if (!card) return;
        const statusEl = card.querySelector('.template-result-card__status');
        if (statusEl) statusEl.textContent = text;
    }

    function fillResultCard(generationId, info, url) {
        const card = resultsGridEl.querySelector('[data-generation-id="' + cssEscape(generationId) + '"]');
        if (!card) return;

        card.classList.remove('template-result-card--pending');
        card.innerHTML =
            '<div class="template-result-card__image-wrap">' +
                '<img src="' + escapeHtml(url) + '" alt="Generated ' + escapeHtml(info.presetId) + '" ' +
                     'class="template-result-card__image" loading="lazy" />' +
            '</div>' +
            '<div class="template-result-card__meta">' +
                '<span class="template-result-card__format">' + escapeHtml(info.presetId) + ' • ' + info.width + '×' + info.height + '</span>' +
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
    // YARDIMCILAR (template-studio.js ile kasıtlı duplike — F.M.10b scope küçük)
    // ═══════════════════════════════════════════════

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
            console.error('[PresetStudio] Balance refresh hatası:', err);
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

    function cssEscape(str) {
        return String(str).replace(/["\\]/g, '\\$&');
    }

    return { init };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', PresetStudio.init);
} else {
    PresetStudio.init();
}
