///**
// * Flux Models Loader Module
// * Sayfa yüklendiğinde API'den Flux modellerini çeker ve cache'ler
// */

//const FluxModelsLoader = (function () {
//    'use strict';

//    // Cache - sayfa kapatılana kadar aynı data kullanılır
//    let cachedFluxModels = null;
//    let isLoading = false;
//    let isLoaded = false;

//    // Referans görseller (mevcut görselleri koruyoruz)
//    const referenceImages = [
//        {
//            img: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=200&fit=crop&crop=face",
//            thumbnail: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=100&h=100&fit=crop&crop=face",
//            grayscale: true
//        },
//        {
//            img: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&h=200&fit=crop&crop=face",
//            thumbnail: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100&h=100&fit=crop&crop=face"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1509631179647-0177331693ae?w=150&h=200&fit=crop&crop=face",
//            thumbnail: "https://images.unsplash.com/photo-1509631179647-0177331693ae?w=100&h=100&fit=crop&crop=face"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=150&h=200&fit=crop&crop=face",
//            thumbnail: "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=100&h=100&fit=crop&crop=face"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&h=200&fit=crop&crop=face",
//            thumbnail: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=100&h=100&fit=crop&crop=face",
//            isNew: true
//        },
//        {
//            img: "https://images.unsplash.com/photo-1509248961406-689dbc7e2b0f?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1509248961406-689dbc7e2b0f?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1604975701397-6365ccbd028a?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1604975701397-6365ccbd028a?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1518882605630-8b57d60260e3?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1518882605630-8b57d60260e3?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1633177317976-3f9bc45e1d1d?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1633177317976-3f9bc45e1d1d?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1560250097-0b93528c311a?w=150&h=200&fit=crop&crop=face",
//            thumbnail: "https://images.unsplash.com/photo-1560250097-0b93528c311a?w=100&h=100&fit=crop&crop=face"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1461896836934- voices-8e8dfdff?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1517649763962-0c623066013b?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1514820720269-e1e4bcd3c8f9?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1514820720269-e1e4bcd3c8f9?w=100&h=100&fit=crop"
//        },
//        {
//            img: "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=150&h=200&fit=crop",
//            thumbnail: "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=100&h=100&fit=crop"
//        }
//    ];

//    /**
//     * Modülü başlat - sayfa yüklendiğinde çağrılır
//     */
//    async function init() {
//        console.log('[FluxModelsLoader] Initializing...');
//        await loadFluxModels();
//    }

//    /**
//     * API'den Flux modellerini çek
//     */
//    async function loadFluxModels() {
//        // Zaten yüklendiyse veya yükleniyorsa tekrar istek atma
//        if (isLoaded || isLoading) {
//            console.log('[FluxModelsLoader] Models already loaded or loading, using cache.');
//            return cachedFluxModels;
//        }

//        isLoading = true;
//        console.log('[FluxModelsLoader] Fetching Flux models from API...');

//        try {
//            const response = await fetch('/RenderNet/GetFluxStyles', {
//                method: 'GET',
//                headers: {
//                    'Content-Type': 'application/json'
//                }
//            });

//            if (!response.ok) {
//                throw new Error(`API Error: ${response.status} ${response.statusText}`);
//            }

//            const result = await response.json();
//            console.log('[FluxModelsLoader] API Response:', result);

//            // API response: { data: [...] }
//            cachedFluxModels = result.data || [];
//            isLoaded = true;
//            isLoading = false;

//            console.log('[FluxModelsLoader] Loaded models:', cachedFluxModels);

//            // UI'ı güncelle
//            updateFluxModelsUI();

//            return cachedFluxModels;

//        } catch (error) {
//            console.error('[FluxModelsLoader] Error loading Flux models:', error);
//            isLoading = false;

//            // Hata durumunda mevcut statik verileri koru (fallback)
//            console.log('[FluxModelsLoader] Falling back to static data.');
//            return null;
//        }
//    }

//    /**
//     * Flux Models UI'ını güncelle
//     */
//    function updateFluxModelsUI() {
//        const fluxContent = document.getElementById('flux-content');
//        if (!fluxContent || !cachedFluxModels || cachedFluxModels.length === 0) {
//            console.warn('[FluxModelsLoader] Cannot update UI - missing content or models.');
//            return;
//        }

//        const gridContainer = fluxContent.querySelector('.grid');
//        if (!gridContainer) {
//            console.warn('[FluxModelsLoader] Grid container not found.');
//            return;
//        }

//        // Grid'i temizle
//        gridContainer.innerHTML = '';

//        // Her model için kart oluştur
//        cachedFluxModels.forEach((model, index) => {
//            // Görselleri döngüsel kullan
//            const refImage = referenceImages[index % referenceImages.length];
//            const modelCard = createModelCard(model, refImage, index);
//            gridContainer.appendChild(modelCard);
//        });

//        // Event listener'ları yeniden bağla
//        rebindModelOptionEvents();

//        console.log('[FluxModelsLoader] UI updated with', cachedFluxModels.length, 'models.');
//    }

//    /**
//     * Model kartı HTML'i oluştur
//     */
//    function createModelCard(model, refImage, index) {
//        const div = document.createElement('div');
//        div.className = 'model-option cursor-pointer group';
//        div.dataset.baseModel = model.base_model || 'flux';
//        div.dataset.name = model.name;
//        div.dataset.img = refImage.thumbnail;

//        const grayscaleClass = refImage.grayscale ? 'grayscale' : '';
//        const newBadge = refImage.isNew
//            ? '<span class="absolute top-1 right-1 bg-green-500 text-white text-[10px] px-1.5 py-0.5 rounded">New</span>'
//            : '';

//        div.innerHTML = `
//            <div class="aspect-[3/4] rounded-lg overflow-hidden border-2 border-transparent hover:border-primary transition-all duration-200 relative">
//                <img src="${refImage.img}"
//                     alt="${model.name}"
//                     class="w-full h-full object-cover ${grayscaleClass}"
//                     loading="lazy" />
//                ${newBadge}
//            </div>
//            <p class="text-xs text-center text-text-secondary mt-1.5 group-hover:text-text-primary">${model.name}</p>
//        `;

//        return div;
//    }

//    /**
//     * Model seçim event'lerini yeniden bağla
//     */
//    function rebindModelOptionEvents() {
//        const modelOptions = document.querySelectorAll('#flux-content .model-option');

//        modelOptions.forEach(option => {
//            option.addEventListener('click', handleModelOptionClick);
//        });
//    }

//    /**
//     * Model seçim click handler
//     */
//    function handleModelOptionClick(e) {
//        const option = e.currentTarget;
//        const modelName = option.dataset.name;
//        const baseModel = option.dataset.baseModel;
//        const modelImg = option.dataset.img;

//        console.log('[FluxModelsLoader] Model selected:', { name: modelName, baseModel: baseModel });

//        // Önceki seçimi kaldır (tüm kategorilerdeki seçimleri temizle)
//        document.querySelectorAll('.model-option').forEach(opt => {
//            opt.classList.remove('selected');
//            const imgContainer = opt.querySelector('.aspect-\$$3\\/4\$$');
//            if (imgContainer) {
//                imgContainer.classList.remove('border-primary');
//                imgContainer.classList.add('border-transparent');
//            }
//        });

//        // Yeni seçimi işaretle
//        option.classList.add('selected');
//        const imgContainer = option.querySelector('.aspect-\$$3\\/4\$$');
//        if (imgContainer) {
//            imgContainer.classList.remove('border-transparent');
//            imgContainer.classList.add('border-primary');
//        }

//        // Hidden input'ları güncelle (Form submission için)
//        updateHiddenInputs(modelName, baseModel);

//        // Sidebar'daki model text'ini güncelle
//        updateSelectedModelText(modelName);

//        // Custom event dispatch et (diğer modüller için)
//        dispatchModelSelectedEvent(modelName, baseModel, modelImg);
//    }

//    /**
//     * Hidden input'ları güncelle
//     */
//    function updateHiddenInputs(modelName, baseModel) {
//        const selectedModelValue = document.getElementById('selectedModelValue');
//        const selectedModelBaseModel = document.getElementById('selectedModelBaseModel');
//        const selectedModelName = document.getElementById('selectedModelName');

//        if (selectedModelValue) selectedModelValue.value = modelName;
//        if (selectedModelBaseModel) selectedModelBaseModel.value = baseModel;
//        if (selectedModelName) selectedModelName.value = modelName;

//        console.log('[FluxModelsLoader] Hidden inputs updated:', {
//            Model: modelName,
//            'StyleDetail.BaseModel': baseModel,
//            'StyleDetail.Name': modelName
//        });
//    }

//    /**
//     * Sidebar'daki seçili model textini güncelle
//     */
//    function updateSelectedModelText(modelName) {
//        const selectedModelText = document.getElementById('selectedModelText');
//        if (selectedModelText) {
//            selectedModelText.textContent = `Selected: ${modelName}`;
//            selectedModelText.classList.remove('text-text-secondary');
//            selectedModelText.classList.add('text-primary');
//        }
//    }

//    /**
//     * Model seçim event'i dispatch et
//     */
//    function dispatchModelSelectedEvent(modelName, baseModel, modelImg) {
//        const event = new CustomEvent('fluxModelSelected', {
//            detail: {
//                name: modelName,
//                baseModel: baseModel,
//                img: modelImg
//            }
//        });
//        document.dispatchEvent(event);
//    }

//    /**
//     * Cache'lenmiş modelleri getir
//     */
//    function getModels() {
//        return cachedFluxModels;
//    }

//    /**
//     * Yükleme durumunu kontrol et
//     */
//    function isModelsLoaded() {
//        return isLoaded;
//    }

//    /**
//     * Belirli bir modeli isimle bul
//     */
//    function getModelByName(name) {
//        if (!cachedFluxModels) return null;
//        return cachedFluxModels.find(m => m.name === name);
//    }

//    /**
//     * Cache'i temizle ve yeniden yükle (gerekirse)
//     */
//    async function refresh() {
//        cachedFluxModels = null;
//        isLoaded = false;
//        isLoading = false;
//        await loadFluxModels();
//    }

//    // Public API
//    return {
//        init,
//        loadFluxModels,
//        getModels,
//        getModelByName,
//        isModelsLoaded,
//        refresh
//    };

//})();

//// Sayfa yüklendiğinde otomatik başlat
//document.addEventListener('DOMContentLoaded', function () {
//    FluxModelsLoader.init();
//});

//// Export for module systems
//if (typeof module !== 'undefined' && module.exports) {
//    module.exports = FluxModelsLoader;
//}







/**
 * Flux Models Loader Module
 * Sayfa yüklendiğinde API'den Flux modellerini çeker ve cache'ler
 */

//const FluxModelsLoader = (function () {
//    'use strict';

//    // Cache - sayfa kapatılana kadar aynı data kullanılır
//    let cachedFluxModels = null;
//    let isLoading = false;
//    let isLoaded = false;

//    // Görsel ayarları
//    const IMAGE_BASE_PATH = '/images/models/flux';
//    const DEFAULT_THUMB = '/images/models/flux/default-thumb.jpg';
//    const DEFAULT_PREVIEW = '/images/models/flux/default-preview.jpg';

//    // Geçici fallback görseller (kendi görsellerinizi ekleyene kadar)
//    const TEMP_FALLBACK_IMAGES = [
//        "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=200&fit=crop&crop=face",
//        "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&h=200&fit=crop&crop=face",
//        "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1509631179647-0177331693ae?w=150&h=200&fit=crop&crop=face",
//        "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=150&h=200&fit=crop&crop=face",
//        "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&h=200&fit=crop&crop=face",
//        "https://images.unsplash.com/photo-1509248961406-689dbc7e2b0f?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1604975701397-6365ccbd028a?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1518882605630-8b57d60260e3?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1633177317976-3f9bc45e1d1d?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1560250097-0b93528c311a?w=150&h=200&fit=crop&crop=face",
//        "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1517649763962-0c623066013b?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1514820720269-e1e4bcd3c8f9?w=150&h=200&fit=crop",
//        "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=150&h=200&fit=crop"
//    ];

//    // Geçici fallback kullanılsın mı? (Kendi görsellerinizi ekleyince false yapın)
//    const USE_TEMP_FALLBACK = true;

//    /**
//     * Model isminden slug oluştur
//     * "Black & White" → "black-and-white"
//     * "Flux TrueTouch" → "flux-truetouch"
//     */
//    function createSlug(modelName) {
//        return modelName
//            .toLowerCase()
//            .replace(/&/g, 'and')
//            .replace(/[^a-z0-9]+/g, '-')
//            .replace(/^-+|-+$/g, '');
//    }

//    /**
//     * Model için görsel URL'i oluştur
//     * @param {string} modelName - Model ismi
//     * @param {string} type - 'thumb' veya 'preview'
//     * @returns {string} Görsel URL'i
//     */
//    function getImageUrl(modelName, type = 'thumb') {
//        const slug = createSlug(modelName);
//        return `${IMAGE_BASE_PATH}/${slug}-${type}.jpg`;
//    }

//    /**
//     * Geçici fallback görsel URL'i al
//     */
//    function getTempFallbackImage(index) {
//        if (!USE_TEMP_FALLBACK) return DEFAULT_THUMB;
//        return TEMP_FALLBACK_IMAGES[index % TEMP_FALLBACK_IMAGES.length];
//    }

//    /**
//     * Modülü başlat - sayfa yüklendiğinde çağrılır
//     */
//    async function init() {
//        console.log('[FluxModelsLoader] Initializing...');
//        await loadFluxModels();
//    }

//    /**
//     * API'den Flux modellerini çek
//     */
//    async function loadFluxModels() {
//        if (isLoaded || isLoading) {
//            console.log('[FluxModelsLoader] Models already loaded or loading, using cache.');
//            return cachedFluxModels;
//        }

//        isLoading = true;
//        console.log('[FluxModelsLoader] Fetching Flux models from API...');

//        try {
//            const response = await fetch('/RenderNet/GetFluxStyles', {
//                method: 'GET',
//                headers: {
//                    'Content-Type': 'application/json'
//                }
//            });

//            if (!response.ok) {
//                throw new Error(`API Error: ${response.status} ${response.statusText}`);
//            }

//            const result = await response.json();
//            console.log('[FluxModelsLoader] API Response:', result);

//            cachedFluxModels = result.data || [];
//            isLoaded = true;
//            isLoading = false;

//            console.log('[FluxModelsLoader] Loaded models:', cachedFluxModels);

//            updateFluxModelsUI();

//            return cachedFluxModels;

//        } catch (error) {
//            console.error('[FluxModelsLoader] Error loading Flux models:', error);
//            isLoading = false;
//            console.log('[FluxModelsLoader] Falling back to static data.');
//            return null;
//        }
//    }

//    /**
//     * Flux Models UI'ını güncelle
//     */
//    function updateFluxModelsUI() {
//        const fluxContent = document.getElementById('flux-content');
//        if (!fluxContent || !cachedFluxModels || cachedFluxModels.length === 0) {
//            console.warn('[FluxModelsLoader] Cannot update UI - missing content or models.');
//            return;
//        }

//        const gridContainer = fluxContent.querySelector('.grid');
//        if (!gridContainer) {
//            console.warn('[FluxModelsLoader] Grid container not found.');
//            return;
//        }

//        gridContainer.innerHTML = '';

//        cachedFluxModels.forEach((model, index) => {
//            const modelCard = createModelCard(model, index);
//            gridContainer.appendChild(modelCard);
//        });

//        rebindModelOptionEvents();

//        console.log('[FluxModelsLoader] UI updated with', cachedFluxModels.length, 'models.');
//    }

//     /**
//     * Model kartı oluştur
//     */
//    function createModelCard(model, index) {
//        const div = document.createElement('div');
//        div.className = 'model-option cursor-pointer group';
//        div.dataset.baseModel = model.base_model || 'flux';
//        div.dataset.name = model.name;

//        // Görsel URL'lerini belirle
//        let previewUrl, thumbUrl;

//        if (USE_TEMP_FALLBACK) {
//            previewUrl = getTempFallbackImage(index);
//            thumbUrl = getTempFallbackImage(index);
//        } else {
//            previewUrl = getImageUrl(model.name, 'preview');
//            thumbUrl = getImageUrl(model.name, 'thumb');
//        }

//        div.dataset.img = thumbUrl;

//        // Özel stiller
//        const isGrayscale = model.name === 'Black & White';
//        const grayscaleClass = isGrayscale ? 'grayscale' : '';

//        // ✅ onerror'da sonsuz döngüyü önlemek için this.onerror=null eklendi
//        div.innerHTML = `
//        <div class="aspect-[3/4] rounded-lg overflow-hidden border-2 border-transparent hover:border-primary transition-all duration-200 relative">
//            <img src="${previewUrl}"
//                 alt="${model.name}"
//                 class="w-full h-full object-cover ${grayscaleClass}"
//                 loading="lazy"
//                 onerror="this.onerror=null; this.style.display='none';" />
//        </div>
//        <p class="text-xs text-center text-text-secondary mt-1.5 group-hover:text-text-primary">${model.name}</p>
//    `;

//        return div;
//    }

//    /**
//     * Model seçim event'lerini yeniden bağla
//     */
//    function rebindModelOptionEvents() {
//        const modelOptions = document.querySelectorAll('#flux-content .model-option');

//        modelOptions.forEach(option => {
//            option.addEventListener('click', handleModelOptionClick);
//        });
//    }

//    /**
//     * Model seçim click handler
//     */
//    function handleModelOptionClick(e) {
//        const option = e.currentTarget;
//        const modelName = option.dataset.name;
//        const baseModel = option.dataset.baseModel;
//        const modelImg = option.dataset.img;

//        console.log('[FluxModelsLoader] Model selected:', { name: modelName, baseModel: baseModel });

//        // Önceki seçimi kaldır
//        document.querySelectorAll('.model-option').forEach(opt => {
//            opt.classList.remove('selected');
//            const imgContainer = opt.querySelector('.aspect-\$$3\\/4\$$');
//            if (imgContainer) {
//                imgContainer.classList.remove('border-primary');
//                imgContainer.classList.add('border-transparent');
//            }
//        });

//        // Yeni seçimi işaretle
//        option.classList.add('selected');
//        const imgContainer = option.querySelector('.aspect-\$$3\\/4\$$');
//        if (imgContainer) {
//            imgContainer.classList.remove('border-transparent');
//            imgContainer.classList.add('border-primary');
//        }

//        // Hidden input'ları güncelle
//        updateHiddenInputs(modelName, baseModel);

//        // Sidebar'daki model text'ini güncelle
//        updateSelectedModelText(modelName);

//        // Custom event dispatch et
//        dispatchModelSelectedEvent(modelName, baseModel, modelImg);
//    }

//    /**
//     * Hidden input'ları güncelle
//     */
//    function updateHiddenInputs(modelName, baseModel) {
//        const selectedModelValue = document.getElementById('selectedModelValue');
//        const selectedModelBaseModel = document.getElementById('selectedModelBaseModel');
//        const selectedModelName = document.getElementById('selectedModelName');

//        if (selectedModelValue) selectedModelValue.value = modelName;
//        if (selectedModelBaseModel) selectedModelBaseModel.value = baseModel;
//        if (selectedModelName) selectedModelName.value = modelName;

//        console.log('[FluxModelsLoader] Hidden inputs updated:', {
//            Model: modelName,
//            'StyleDetail.BaseModel': baseModel,
//            'StyleDetail.Name': modelName
//        });
//    }

//    /**
//     * Sidebar'daki seçili model textini güncelle
//     */
//    function updateSelectedModelText(modelName) {
//        const selectedModelText = document.getElementById('selectedModelText');
//        if (selectedModelText) {
//            selectedModelText.textContent = `Selected: ${modelName}`;
//            selectedModelText.classList.remove('text-text-secondary');
//            selectedModelText.classList.add('text-primary');
//        }
//    }

//    /**
//     * Model seçim event'i dispatch et
//     */
//    function dispatchModelSelectedEvent(modelName, baseModel, modelImg) {
//        const event = new CustomEvent('fluxModelSelected', {
//            detail: {
//                name: modelName,
//                baseModel: baseModel,
//                img: modelImg
//            }
//        });
//        document.dispatchEvent(event);
//    }

//    /**
//     * Cache'lenmiş modelleri getir
//     */
//    function getModels() {
//        return cachedFluxModels;
//    }

//    /**
//     * Yükleme durumunu kontrol et
//     */
//    function isModelsLoaded() {
//        return isLoaded;
//    }

//    /**
//     * Belirli bir modeli isimle bul
//     */
//    function getModelByName(name) {
//        if (!cachedFluxModels) return null;
//        return cachedFluxModels.find(m => m.name === name);
//    }

//    /**
//     * Cache'i temizle ve yeniden yükle
//     */
//    async function refresh() {
//        cachedFluxModels = null;
//        isLoaded = false;
//        isLoading = false;
//        await loadFluxModels();
//    }

//    // Public API
//    return {
//        init,
//        loadFluxModels,
//        getModels,
//        getModelByName,
//        isModelsLoaded,
//        refresh,
//        createSlug,        // Dışarıdan da erişilebilir (debug için)
//        getImageUrl        // Dışarıdan da erişilebilir
//    };

//})();

//// Sayfa yüklendiğinde otomatik başlat
//document.addEventListener('DOMContentLoaded', function () {
//    FluxModelsLoader.init();
//});

//// Export for module systems
//if (typeof module !== 'undefined' && module.exports) {
//    module.exports = FluxModelsLoader;
//}




/**
 * Flux Models Loader Module
 * Sayfa yüklendiğinde API'den Flux modellerini çeker ve cache'ler
 */

const FluxModelsLoader = (function () {
    'use strict';

    // Cache
    let cachedFluxModels = null;
    let isLoading = false;
    let isLoaded = false;

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
        await loadFluxModels();
    }

    /**
     * API'den Flux modellerini çek
     */
    async function loadFluxModels() {
        if (isLoaded || isLoading) {
            console.log('[FluxModelsLoader] Models already loaded or loading, using cache.');
            return cachedFluxModels;
        }

        isLoading = true;
        console.log('[FluxModelsLoader] Fetching Flux models from API...');

        try {
            const response = await fetch('/RenderNet/GetFluxStyles', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`API Error: ${response.status} ${response.statusText}`);
            }

            const result = await response.json();
            console.log('[FluxModelsLoader] API Response:', result);

            cachedFluxModels = result.data || [];
            isLoaded = true;
            isLoading = false;

            console.log('[FluxModelsLoader] Loaded models:', cachedFluxModels);

            updateFluxModelsUI();

            return cachedFluxModels;

        } catch (error) {
            console.error('[FluxModelsLoader] Error loading Flux models:', error);
            isLoading = false;
            console.log('[FluxModelsLoader] Falling back to static data.');
            return null;
        }
    }

    /**
     * Flux Models UI'ını güncelle
     */
    function updateFluxModelsUI() {
        const fluxContent = document.getElementById('flux-content');
        if (!fluxContent || !cachedFluxModels || cachedFluxModels.length === 0) {
            console.warn('[FluxModelsLoader] Cannot update UI - missing content or models.');
            return;
        }

        const gridContainer = fluxContent.querySelector('.grid');
        if (!gridContainer) {
            console.warn('[FluxModelsLoader] Grid container not found.');
            return;
        }

        gridContainer.innerHTML = '';

        cachedFluxModels.forEach((model, index) => {
            const modelCard = createModelCard(model, index);
            gridContainer.appendChild(modelCard);
        });

        // ✅ ModelSelectionPanel'e yeni elementleri tanıt
        if (typeof ModelSelectionPanel !== 'undefined' && ModelSelectionPanel.refreshModelOptions) {
            ModelSelectionPanel.refreshModelOptions();
        }

        console.log('[FluxModelsLoader] UI updated with', cachedFluxModels.length, 'models.');
    }

    /**
     * Model kartı oluştur
     */
    function createModelCard(model, index) {
        const div = document.createElement('div');
        div.className = 'model-option cursor-pointer group';
        div.dataset.baseModel = model.base_model || 'flux';
        div.dataset.name = model.name;

        let previewUrl, thumbUrl;

        if (USE_TEMP_FALLBACK) {
            previewUrl = getTempFallbackImage(index);
            thumbUrl = getTempFallbackImage(index);
        } else {
            previewUrl = getImageUrl(model.name, 'preview');
            thumbUrl = getImageUrl(model.name, 'thumb');
        }

        div.dataset.img = thumbUrl;

        const isGrayscale = model.name === 'Black & White';
        const grayscaleClass = isGrayscale ? 'grayscale' : '';

        div.innerHTML = `
            <div class="aspect-[3/4] rounded-lg overflow-hidden border-2 border-transparent hover:border-primary transition-all duration-200 relative">
                <img src="${previewUrl}" 
                     alt="${model.name}" 
                     class="w-full h-full object-cover ${grayscaleClass}"
                     loading="lazy"
                     onerror="this.onerror=null; this.style.display='none';" />
            </div>
            <p class="text-xs text-center text-text-secondary mt-1.5 group-hover:text-text-primary">${model.name}</p>
        `;

        return div;
    }

    /**
     * Cache'lenmiş modelleri getir
     */
    function getModels() {
        return cachedFluxModels;
    }

    /**
     * Yükleme durumunu kontrol et
     */
    function isModelsLoaded() {
        return isLoaded;
    }

    /**
     * Belirli bir modeli isimle bul
     */
    function getModelByName(name) {
        if (!cachedFluxModels) return null;
        return cachedFluxModels.find(m => m.name === name);
    }

    /**
     * Cache'i temizle ve yeniden yükle
     */
    async function refresh() {
        cachedFluxModels = null;
        isLoaded = false;
        isLoading = false;
        await loadFluxModels();
    }

    // Public API
    return {
        init,
        loadFluxModels,
        getModels,
        getModelByName,
        isModelsLoaded,
        refresh,
        createSlug,
        getImageUrl
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