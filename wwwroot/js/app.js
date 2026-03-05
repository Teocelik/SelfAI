const App = (function () {
    'use strict';

    let uploadForm = null;
    let signalRConnection = null;

    // ═══════════════════════════════════════════════
    // 🆕 CLIENT ID YÖNETİMİ
    // Kullanıcıyı tanımak için benzersiz ID
    // localStorage'da saklanır, tarayıcı kapansa bile kalır
    // ═══════════════════════════════════════════════

    /**
     * Client ID al veya oluştur
     * 
     * 💡 NEDEN localStorage?
     * - Session → Tarayıcı kapanınca silinir ❌
     * - Cookie → Kullanıcı silebilir, süre dolabilir ❌
     * - localStorage → Tarayıcı kapansa bile KALIR ✅
     *                  Kullanıcı manuel silmedikçe kaybolmaz
     * 
     * İleride auth sistemi eklendiğinde:
     *   clientId → userId ile değiştirilir
     */
    function getClientId() {
        let clientId = localStorage.getItem('selfai_client_id');

        if (!clientId) {
            // Benzersiz ID oluştur (UUID v4 formatı)
            clientId = 'client_' + crypto.randomUUID();
            localStorage.setItem('selfai_client_id', clientId);
            console.log('[ClientId] Yeni oluşturuldu:', clientId);
        }

        return clientId;
    }

    // ═══════════════════════════════════════════════

    function init() {
        uploadForm = document.getElementById('uploadForm');

        initPromptHandler();
        initFaceLockPanel();
        initFluxImageStyles();
        initModelSelectionPanel();
        initImageControls();
        initFormHandler();
        initSignalR();

        console.log('AI Image Generation Studio initialized successfully');
        console.log('[ClientId]:', getClientId());
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
                const firstImage = data.media.find(m => m.status === 'success' && m.url);

                if (firstImage) {
                    Toast.success('Görsel başarıyla oluşturuldu!', 'Tamamlandı 🎨');
                    ImageControls.showGeneratedImage(firstImage.url);
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

        // 🆕 Yeniden bağlandığında kendini tanıt + bekleyen sonuçları al
        signalRConnection.onreconnected(function () {
            console.log('[SignalR] Yeniden bağlandı!');
            registerWithServer();
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

            // 🆕 Bağlantı kurulduğunda kendini tanıt
            await registerWithServer();
        } catch (err) {
            console.error('[SignalR] Bağlantı hatası:', err);
            setTimeout(startSignalRConnection, 5000);
        }
    }

    /**
     * 🆕 Sunucuya clientId ile kendini tanıt
     * 
     * Bu çağrı şunları tetikler:
     * 1. Backend clientId → connectionId mapping'ini günceller
     * 2. Bekleyen sonuçlar varsa ANINDA gönderir
     * 
     * Ne zaman çağrılır:
     * - İlk bağlantıda
     * - Yeniden bağlantıda (sayfa yenileme, internet kopması vb.)
     */
    async function registerWithServer() {
        try {
            const clientId = getClientId();
            await signalRConnection.invoke('RegisterClient', clientId);
            console.log('[SignalR] Server\'a kayıt olundu. ClientId:', clientId);
        } catch (err) {
            console.error('[SignalR] Kayıt hatası:', err);
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
        const clientId = getClientId();

        if (!connectionId) {
            Toast.error('Sunucu ile bağlantı kurulamadı. Sayfayı yenileyin.', 'Bağlantı Hatası');
            return;
        }

        ImageControls.showLoadingState();
        ImageControls.setGenerateButtonState(true);

        const formData = new FormData(uploadForm);

        // 🆕 Hem clientId hem connectionId gönder
        const result = await apiFetch('/RenderNet/GenerateImage', {
            method: 'POST',
            body: formData,
            headers: {
                'X-SignalR-ConnectionId': connectionId,
                'X-Client-Id': clientId                    // 🆕
            }
        });

        if (result && result.generationId) {
            Toast.info('Görsel oluşturuluyor, lütfen bekleyin...', 'İşleniyor');
            // SignalR bildirim gönderecek, bekliyoruz...
        } else {
            ImageControls.showDefaultState();
            ImageControls.setGenerateButtonState(false);
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
        getClientId,     // 🆕
        getConnectionId
    };
})();

document.addEventListener('DOMContentLoaded', function () {
    App.init();
});