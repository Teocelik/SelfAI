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
        initModelSelectionPanel();
        initImageControls();
        initSeedHandler();
        initCharacterPanel();
        initPoseLockPanel();
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

        // ✅ Görsel TAMAMLANDI
        signalRConnection.on('GenerationCompleted', function (data) {
            console.log('[SignalR] GenerationCompleted:', data);

            ImageControls.setGenerateButtonState(false);

            if (data.media && data.media.length > 0) {
                const successfulImages = data.media
                    .filter(m => m.status === 'success' && m.url)
                    .map(m => m.url);

                if (successfulImages.length > 0) {
                    const message = successfulImages.length === 1
                        ? 'Görsel başarıyla oluşturuldu!'
                        : `${successfulImages.length} görsel başarıyla oluşturuldu!`;
                    Toast.success(message, 'Tamamlandı 🎨');
                    ImageControls.showGeneratedImages(successfulImages);
                } else {
                    Toast.error('Görsel URL\'i alınamadı.');
                    ImageControls.showDefaultState();
                }
            }
        });

        // ❌ BAŞARISIZ
        signalRConnection.on('GenerationFailed', function (data) {
            console.log('[SignalR] GenerationFailed:', data);
            Toast.error(data.message || 'Görsel oluşturulurken bir hata oluştu.', 'Başarısız');
            ImageControls.showDefaultState();
            ImageControls.setGenerateButtonState(false);
        });

        // ⏰ ZAMAN AŞIMI
        signalRConnection.on('GenerationTimeout', function (data) {
            console.log('[SignalR] GenerationTimeout:', data);
            Toast.warning(data.message || 'Zaman aşımına uğradı.', 'Zaman Aşımı');
            ImageControls.showDefaultState();
            ImageControls.setGenerateButtonState(false);
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

        // Random modda her Generate'te yeni seed üret (custom modda kullanıcının değeri korunur)
        if (typeof SeedHandler !== 'undefined') {
            const refreshedSeed = SeedHandler.refreshIfRandom();
            if (refreshedSeed) console.log('Random seed yenilendi:', refreshedSeed);
        }

        const formData = new FormData(uploadForm);

        // 🆕 Character mention dönüşümü: UI'da @Name görünür, backend'e {Name} gider (RenderNet formatı)
        if (typeof CharacterPanel !== 'undefined') {
            const originalPrompt = formData.get('PositivePrompt');
            const transformedPrompt = CharacterPanel.transformPromptForSubmit(originalPrompt);
            if (transformedPrompt !== originalPrompt) {
                formData.set('PositivePrompt', transformedPrompt);
                console.log('[Character] Prompt @Name → {Name} dönüştürüldü');
            }
        }

        // Kimlik auth çereziyle gider; yalnızca SignalR connectionId header'ı gerekir.
        const result = await apiFetch('/RenderNet/GenerateImage', {
            method: 'POST',
            body: formData,
            headers: {
                'X-SignalR-ConnectionId': connectionId
            }
        });

        if (result && result.generationId) {
            Toast.info('Görsel oluşturuluyor, lütfen bekleyin...', 'İşleniyor');
            // Kredi düşüldü — top bar bakiyesini güncelle (sunucu currentBalance döndü)
            refreshCreditBalance();
            // SignalR bildirim gönderecek, bekliyoruz...
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

    function initModelSelectionPanel() {
        if (typeof ModelSelectionPanel !== 'undefined') ModelSelectionPanel.init();
        else console.warn('ModelSelectionPanel module not found');
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

    function resetAll() {
        if (typeof PromptHandler !== 'undefined') PromptHandler.clear();
        if (typeof FaceLockPanel !== 'undefined') FaceLockPanel.reset();
        if (typeof FluxImageStyles !== 'undefined') FluxImageStyles.reset();
        if (typeof ModelSelectionPanel !== 'undefined') ModelSelectionPanel.reset();
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