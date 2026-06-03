
/**
 * Flux Models Loader Module
 * Sayfa yüklendiğinde API'den Flux modellerini çeker ve cache'ler
 */

const FluxModelsLoader = (function () {
    'use strict';

    // Cache (tip bazlı: flux / sdxl / sd)
    let cachedModelsByType = { flux: [], sdxl: [], sd: [] };
    let cachedStylesByType = { flux: [], sdxl: [], sd: [] };
    let loadedTypes = new Set();

    // Görsel ayarları
    const IMAGE_BASE_PATH = '/images/models/flux';
    const DEFAULT_THUMB = '/images/models/flux/default-thumb.jpg';

    // Geçici fallback görseller
    const TEMP_FALLBACK_IMAGES = [
        "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=200&fit=crop&crop=face&auto=format",
        "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&h=200&fit=crop&crop=face&auto=format",
        "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1509631179647-0177331693ae?w=150&h=200&fit=crop&crop=face&auto=format",
        "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=150&h=200&fit=crop&crop=face&auto=format",
        "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&h=200&fit=crop&crop=face&auto=format",
        "https://images.unsplash.com/photo-1509248961406-689dbc7e2b0f?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1604975701397-6365ccbd028a?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1518882605630-8b57d60260e3?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1633177317976-3f9bc45e1d1d?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1560250097-0b93528c311a?w=150&h=200&fit=crop&crop=face&auto=format",
        "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1517649763962-0c623066013b?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1514820720269-e1e4bcd3c8f9?w=150&h=200&fit=crop&auto=format",
        "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=150&h=200&fit=crop&auto=format"
    ];

    const USE_TEMP_FALLBACK = true;

    /**
     * Model isminden slug oluştur
     */
    function createSlug(modelName) {
        return modelName
            .toLowerCase()
            .replace(/&/g, 'and')
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '');
    }

    /**
     * Model için görsel URL'i oluştur
     */
    function getImageUrl(modelName, type = 'thumb') {
        const slug = createSlug(modelName);
        return `${IMAGE_BASE_PATH}/${slug}-${type}.jpg`;
    }

    /**
     * Geçici fallback görsel URL'i al
     */
    function getTempFallbackImage(index) {
        if (!USE_TEMP_FALLBACK) return DEFAULT_THUMB;
        return TEMP_FALLBACK_IMAGES[index % TEMP_FALLBACK_IMAGES.length];
    }

    /**
     * Modülü başlat
     */
    async function init() {
        console.log('[FluxModelsLoader] Initializing...');
        // Şimdilik yalnızca flux tipini yükle; SDXL ileride eklenir.
        await loadModelsForType('flux');
    }

    /**
     * Belirli bir tip için modelleri VE stilleri paralel çek, cross-product ile kart üret
     */
    async function loadModelsForType(type) {
        if (loadedTypes.has(type)) {
            console.log('[ModelsLoader] Already loaded:', type);
            return;
        }

        console.log('[ModelsLoader] Fetching models + styles for type:', type);

        try {
            // Modeller ve stiller paralel çekilir
            const [modelsRes, stylesRes] = await Promise.all([
                fetch(`/RenderNet/GetModels?type=${type}`),
                fetch(`/RenderNet/GetStyles?type=${type}`)
            ]);

            if (!modelsRes.ok || !stylesRes.ok) {
                throw new Error(`API Error: ${modelsRes.status}, ${stylesRes.status}`);
            }

            const modelsJson = await modelsRes.json();
            const stylesJson = await stylesRes.json();

            cachedModelsByType[type] = modelsJson.data || [];
            cachedStylesByType[type] = stylesJson.data || [];
            loadedTypes.add(type);

            console.log('[ModelsLoader] Loaded:', type,
                '| models:', cachedModelsByType[type].length,
                '| styles:', cachedStylesByType[type].length);

            updateUIForType(type);

        } catch (err) {
            console.error('[ModelsLoader] Error loading type:', type, err);
            // Hata durumunda HTML'deki hardcoded kartlar fallback olarak kalır.
        }
    }

    /**
     * Belirli bir tipin UI'ını güncelle — her (model, style) çifti bir kart
     */
    function updateUIForType(type) {
        const container = document.getElementById(`${type}-content`);
        const models = cachedModelsByType[type] || [];
        const styles = cachedStylesByType[type] || [];

        if (!container || models.length === 0 || styles.length === 0) {
            console.warn('[ModelsLoader] Cannot update UI - missing content, models or styles.', type);
            return;
        }

        const gridContainer = container.querySelector('.grid');
        if (!gridContainer) {
            console.warn('[ModelsLoader] Grid container not found for type:', type);
            return;
        }

        gridContainer.innerHTML = '';

        // Cross-product: her model × her style → ayrı kart
        let index = 0;
        for (const model of models) {
            for (const style of styles) {
                const card = createModelCard(model, style, index);
                gridContainer.appendChild(card);
                index++;
            }
        }

        // ✅ ModelSelectionPanel'e yeni elementleri tanıt
        if (typeof ModelSelectionPanel !== 'undefined' && ModelSelectionPanel.refreshModelOptions) {
            ModelSelectionPanel.refreshModelOptions();
        }

        console.log('[ModelsLoader] UI updated for', type, 'with', index, 'cards.');
    }

    /**
     * (model, style) çiftinden kart oluştur.
     * Kartın görünür adı style adıdır (Flux tek olduğu için kartlar style ile anılır).
     */
    function createModelCard(model, style, index) {
        const div = document.createElement('div');
        div.className = 'model-option cursor-pointer group';
        div.dataset.model = model.name;                  // örn: "Flux"
        div.dataset.style = style.name;                  // örn: "Cinematic"
        div.dataset.baseModel = model.base_model || 'flux';
        div.dataset.name = style.name;                   // kart görünür adı = style adı

        let previewUrl, thumbUrl;

        if (USE_TEMP_FALLBACK) {
            previewUrl = getTempFallbackImage(index);
            thumbUrl = getTempFallbackImage(index);
        } else {
            previewUrl = getImageUrl(style.name, 'preview');
            thumbUrl = getImageUrl(style.name, 'thumb');
        }

        div.dataset.img = thumbUrl;

        const isGrayscale = style.name === 'Black & White';
        const grayscaleClass = isGrayscale ? 'grayscale' : '';

        div.innerHTML = `
            <div class="aspect-[3/4] rounded-lg overflow-hidden border-2 border-transparent hover:border-primary transition-all duration-200 relative">
                <img src="${previewUrl}"
                     alt="${style.name}"
                     class="w-full h-full object-cover ${grayscaleClass}"
                     loading="lazy"
                     onerror="this.onerror=null; this.style.display='none';" />
            </div>
            <p class="text-xs text-center text-text-secondary mt-1.5 group-hover:text-text-primary">${style.name}</p>
        `;

        return div;
    }

    /**
     * Belirli bir tipin cache'ini temizle ve yeniden yükle
     */
    async function refresh(type = 'flux') {
        cachedModelsByType[type] = [];
        cachedStylesByType[type] = [];
        loadedTypes.delete(type);
        await loadModelsForType(type);
    }

    // Public API
    return {
        init,
        loadModelsForType,
        refresh
    };

})();

// Sayfa yüklendiğinde otomatik başlat
document.addEventListener('DOMContentLoaded', function () {
    FluxModelsLoader.init();
});

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FluxModelsLoader;
}