const App = (function () {
    'use strict';

    let uploadForm = null;
    let signalRConnection = null;

    // ═══════════════════════════════════════════════
    // KULLANICI KİMLİĞİ
    // clientId mekanizması kaldırıldı. Kimlik artık auth çereziyle
    // (Firebase UID) sunucu tarafında belirlenir. SignalR ve fetch
    // çağrıları çerezi otomatik gönderir; ekstra header gerekmez.
    // ═══════════════════════════════════════════════

    function init() {
        uploadForm = document.getElementById('uploadForm');

        initPromptHandler();
        initFaceLockPanel();
        initFluxImageStyles();
        initModelPicker();
        initImageControls();
        initSeedHandler();
        initCharacterPanel();
        initPoseLockPanel();
        initGenerateButtonState();
        initFormHandler();
        initSignalR();

        console.log('AI Image Generation Studio initialized successfully');
    }

    // ═══════════════════════════════════════════════
    // SIGNALR
    // ═══════════════════════════════════════════════

    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR library not loaded');
            return;
        }

        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl('/generationHub')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // ═══ SUNUCUDAN GELEN EVENT'LER ═══

        // 🆕 F.M.3: Tek event — GenerationOrchestrator status ile push eder.
        // payload: { generationId, status: "Completed"|"Failed", images:[{url,width,height}], errorMessage }
        signalRConnection.on('GenerationUpdate', function (payload) {
            console.log('[SignalR] GenerationUpdate:', payload);

            ImageControls.setGenerateButtonState(false);

            if (payload.status === 'Completed') {
                const urls = (payload.images || [])
                    .filter(i => i && i.url)
                    .map(i => i.url);

                if (urls.length > 0) {
                    const message = urls.length === 1
                        ? 'Görsel başarıyla oluşturuldu!'
                        : `${urls.length} görsel başarıyla oluşturuldu!`;
                    Toast.success(message, 'Tamamlandı 🎨');
                    ImageControls.showGeneratedImages(urls);
                } else {
                    Toast.error('Görsel URL\'i alınamadı.');
                    ImageControls.showDefaultState();
                }
            } else {
                // Failed (veya bilinmeyen status) — krediler orchestrator tarafından iade edildi
                Toast.error(payload.errorMessage || 'Görsel oluşturulurken bir hata oluştu.', 'Başarısız');
                ImageControls.showDefaultState();
                // Kredi iadesini top bar'a yansıt
                refreshCreditBalance();
            }
        });

        // 🆕 F.M.4: Karakter LoRA training durum güncellemesi.
        // payload: { characterId, status: "Ready"|"Failed", name, thumbnailUrl?, reason? }
        signalRConnection.on('CharacterTrainingUpdate', function (payload) {
            console.log('[SignalR] CharacterTrainingUpdate:', payload);

            const name = payload.name || 'Karakter';

            if (payload.status === 'Ready') {
                Toast.success('"' + name + '" karakteri hazır! Artık üretimde kullanabilirsin.', 'Karakter Hazır 🎭');
            } else {
                Toast.error(payload.reason || ('"' + name + '" eğitimi başarısız oldu.'), 'Eğitim Başarısız');
            }

            // Modal açıksa kart listesini tazele (badge güncellensin). CharacterModal public API.
            if (typeof CharacterModal !== 'undefined' && typeof CharacterModal.refresh === 'function') {
                const modal = document.getElementById('characterModal');
                if (modal && modal.classList.contains('is-open')) {
                    CharacterModal.refresh();
                }
            }
        });

        // ═══ BAĞLANTI DURUMLARI ═══

        signalRConnection.onreconnecting(function () {
            console.log('[SignalR] Yeniden bağlanılıyor...');
        });

        // Yeniden bağlanınca sunucu OnConnectedAsync'i otomatik tetikler
        // (auth çerezi üzerinden userId çözülür, bekleyen sonuçlar teslim edilir).
        // Frontend'in manuel kayıt yapmasına gerek yok.
        signalRConnection.onreconnected(function () {
            console.log('[SignalR] Yeniden bağlandı!');
        });

        signalRConnection.onclose(function () {
            console.log('[SignalR] Bağlantı kapandı.');
        });

        startSignalRConnection();
    }

    async function startSignalRConnection() {
        try {
            await signalRConnection.start();
            console.log('[SignalR] Bağlantı kuruldu. ConnectionId:', signalRConnection.connectionId);
            // Kayıt + bekleyen sonuç teslimi sunucuda OnConnectedAsync ile otomatik yapılır.
        } catch (err) {
            console.error('[SignalR] Bağlantı hatası:', err);
            setTimeout(startSignalRConnection, 5000);
        }
    }

    function getConnectionId() {
        return signalRConnection?.connectionId || null;
    }

    // ═══════════════════════════════════════════════
    // FORM SUBMIT
    // ═══════════════════════════════════════════════

    function initFormHandler() {
        if (!uploadForm) return;
        uploadForm.addEventListener('submit', handleFormSubmit);
    }

    async function handleFormSubmit(e) {
        e.preventDefault();

        if (!validateForm()) return;

        const connectionId = getConnectionId();

        if (!connectionId) {
            Toast.error('Sunucu ile bağlantı kurulamadı. Sayfayı yenileyin.', 'Bağlantı Hatası');
            return;
        }

        ImageControls.showLoadingState();
        ImageControls.setGenerateButtonState(true);

        // 🆕 F.M.4/F.M.6: Payload modelEndpoint + prompt + aspectRatio (+ numImages) +
        // opsiyonel characterId/characterMode (LoRA) veya faceImageUrl (PuLID) veya
        // poseImageUrl (ControlNet). Üçü mutex; biri set ise backend endpoint'i override eder.
        const promptInput = document.getElementById('promptInput');
        const modelInput = document.getElementById('selectedModelValue');
        const aspectSelect = document.querySelector('select[name="AspectRatio"]');
        const imageCountInput = document.getElementById('imageCount');
        const characterIdInput = document.getElementById('characterId');
        const characterModeInput = document.getElementById('characterMode');

        const payload = {
            modelEndpoint: modelInput ? modelInput.value.trim() : '',
            prompt: promptInput ? promptInput.value.trim() : '',
            aspectRatio: aspectSelect ? aspectSelect.value : '1:1',
            numImages: imageCountInput ? parseInt(imageCountInput.value, 10) || 1 : 1
        };

        // Karakter seçiliyse payload'a ekle (boşsa gönderme — null/undefined backend'de yok sayılır)
        const characterId = characterIdInput ? characterIdInput.value.trim() : '';
        if (characterId) {
            payload.characterId = characterId;
            payload.characterMode = characterModeInput ? characterModeInput.value : 'balanced';
        }

        // F.M.7: Face Lock seçiliyse Asset ID gönderilir (backend URL'e resolve eder). Weight sabit 1.0.
        const faceAssetId = window.FaceLockPanel && window.FaceLockPanel.getFaceAssetId
            ? window.FaceLockPanel.getFaceAssetId() : null;
        if (faceAssetId) {
            payload.faceAssetId = faceAssetId;
            payload.faceWeight = 1.0;
        }

        // F.M.6: Pose Lock seçiliyse ControlNet'e yönlendir (weight F.M.6'da sabit 0.6).
        const poseImageUrl = window.PoseLockPanel && window.PoseLockPanel.getPoseImageUrl
            ? window.PoseLockPanel.getPoseImageUrl() : null;
        if (poseImageUrl) {
            payload.poseImageUrl = poseImageUrl;
            payload.poseWeight = 0.6;
        }

        // Kimlik auth çereziyle gider; SignalR connectionId header'ı + JSON content-type gerekir.
        const result = await apiFetch('/RenderNet/GenerateImage', {
            method: 'POST',
            body: JSON.stringify(payload),
            headers: {
                'Content-Type': 'application/json',
                'X-SignalR-ConnectionId': connectionId
            }
        });

        if (result && result.data && result.data.generationId) {
            Toast.info('Görsel oluşturuluyor, lütfen bekleyin...', 'İşleniyor');
            // Kredi düşüldü — top bar bakiyesini güncelle
            refreshCreditBalance();
            // SignalR "GenerationUpdate" bildirimi bekleniyor...
        } else {
            ImageControls.showDefaultState();
            ImageControls.setGenerateButtonState(false);
        }
    }

    // ═══════════════════════════════════════════════
    // KREDİ BAKİYESİ GÖSTERGESİ (top bar)
    // ═══════════════════════════════════════════════

    // Sunucudan güncel bakiyeyi çekip top bar'daki göstergeyi günceller.
    // Hatalar sessizce yutulur — bakiye göstergesi kritik akış değil.
    async function refreshCreditBalance() {
        const valueEl = document.getElementById('creditBalanceValue');
        if (!valueEl) return; // Kullanıcı login değilse gösterge yok

        try {
            const response = await fetch('/Account/Balance');
            if (response.ok) {
                const data = await response.json();
                if (data.success && data.balance !== undefined) {
                    valueEl.textContent = data.balance;
                }
            }
        } catch (err) {
            console.error('Balance refresh hatası:', err);
        }
    }

    function validateForm() {
        if (typeof PromptHandler !== 'undefined' && !PromptHandler.validate()) {
            Toast.warning('Lütfen bir prompt girin.', 'Eksik Bilgi');
            return false;
        }

        if (typeof PromptHandler === 'undefined') {
            const prompt = document.getElementById('promptInput');
            if (!prompt || !prompt.value.trim()) {
                Toast.warning('Lütfen bir prompt girin.', 'Eksik Bilgi');
                prompt?.focus();
                return false;
            }
        }

        // Defansif seçim kontrolü — buton zaten disabled olmalı, ama DevTools'tan
        // disabled kaldırılırsa backend'e geçersiz istek gitmesin. F.M.6 hotfix-3:
        // model ZORUNLU DEĞİL — Character veya Face Lock aktifse backend endpoint'i
        // override eder (flux-lora / flux-pulid). Üçünden biri yeterli.
        const selectedModel = document.getElementById('selectedModelValue');
        const modelSelected = !!(selectedModel && selectedModel.value.trim());
        const characterSelected = !!(window.CharacterPanel
            && window.CharacterPanel.getSelectedCharacterId
            && window.CharacterPanel.getSelectedCharacterId());
        const faceLockActive = !!(window.FaceLockPanel
            && window.FaceLockPanel.getFaceAssetId
            && window.FaceLockPanel.getFaceAssetId());

        if (!modelSelected && !characterSelected && !faceLockActive) {
            Toast.warning('Lütfen bir model, karakter veya Face Lock seç', 'Eksik Bilgi');
            return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════
    // MODÜL BAŞLATMA
    // ═══════════════════════════════════════════════

    function initPromptHandler() {
        if (typeof PromptHandler !== 'undefined') PromptHandler.init();
        else console.warn('PromptHandler module not found');
    }

    function initFaceLockPanel() {
        if (typeof FaceLockPanel !== 'undefined') FaceLockPanel.init();
        else console.warn('FaceLockPanel module not found');
    }

    function initFluxImageStyles() {
        if (typeof FluxImageStyles !== 'undefined') FluxImageStyles.init();
        else console.warn('FluxImageStyles module not found');
    }

    function initModelPicker() {
        if (typeof ModelPicker !== 'undefined') ModelPicker.init();
        else console.warn('ModelPicker module not found');
    }

    function initImageControls() {
        if (typeof ImageControls !== 'undefined') ImageControls.init();
        else console.warn('ImageControls module not found');
    }

    function initSeedHandler() {
        if (typeof SeedHandler !== 'undefined') SeedHandler.init();
        else console.warn('SeedHandler module not found');
    }

    function initCharacterPanel() {
        if (typeof CharacterPanel !== 'undefined') CharacterPanel.init();
        else console.warn('CharacterPanel module not found');
    }

    function initPoseLockPanel() {
        if (typeof PoseLockPanel !== 'undefined') PoseLockPanel.init();
        else console.warn('PoseLockPanel module not found');
    }

    function initGenerateButtonState() {
        if (typeof GenerateButtonState !== 'undefined') GenerateButtonState.init();
        else console.warn('GenerateButtonState module not found');
    }

    function resetAll() {
        if (typeof PromptHandler !== 'undefined') PromptHandler.clear();
        if (typeof FaceLockPanel !== 'undefined') FaceLockPanel.reset();
        if (typeof FluxImageStyles !== 'undefined') FluxImageStyles.reset();
        if (typeof ModelPicker !== 'undefined') ModelPicker.reset();
        if (typeof ImageControls !== 'undefined') ImageControls.showDefaultState();
    }

    return {
        init,
        resetAll,
        getConnectionId,
        refreshCreditBalance
    };
})();

document.addEventListener('DOMContentLoaded', function () {
    App.init();

    // Sayfa yüklenince kredi bakiyesini çek (gösterge varsa)
    if (document.getElementById('creditBalance')) {
        App.refreshCreditBalance();
    }
});