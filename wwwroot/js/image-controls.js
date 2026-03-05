///**
// * Image Controls Module
// * Handles image count, sliders, and generation state
// */

//const ImageControls = (function () {
//    'use strict';

//    // DOM Elements
//    let decreaseCount = null;
//    let increaseCount = null;
//    let imageCount = null;
//    let generateBtn = null;
//    let clearAllBtn = null;
//    let defaultState = null;
//    let loadingState = null;
//    let imageContainer = null;
//    let generatedImage = null;

//    // Configuration
//    const MIN_COUNT = 1;
//    const MAX_COUNT = 4;

//    /**
//     * Initialize image controls
//     */
//    function init() {
//        cacheElements();
//        bindEvents();
//        initializeSliders();
//    }

//    /**
//     * Cache DOM elements
//     */
//    function cacheElements() {
//        decreaseCount = document.getElementById('decreaseCount');
//        increaseCount = document.getElementById('increaseCount');
//        imageCount = document.getElementById('imageCount');
//        generateBtn = document.getElementById('generateBtn');
//        clearAllBtn = document.getElementById('clearAllBtn');
//        defaultState = document.getElementById('defaultState');
//        loadingState = document.getElementById('loadingState');
//        imageContainer = document.getElementById('imageContainer');
//        generatedImage = document.getElementById('generatedImage');
//    }

//    /**
//     * Bind event listeners
//     */
//    function bindEvents() {
//        if (decreaseCount) {
//            decreaseCount.addEventListener('click', handleDecrease);
//        }

//        if (increaseCount) {
//            increaseCount.addEventListener('click', handleIncrease);
//        }

//        if (generateBtn) {
//            generateBtn.addEventListener('click', handleGenerate);
//        }

//        if (clearAllBtn) {
//            clearAllBtn.addEventListener('click', handleClearAll);
//        }
//    }

//    /**
//     * Initialize all sliders
//     */
//    function initializeSliders() {
//        const sliders = document.querySelectorAll('input[type="range"]');
//        sliders.forEach(slider => {
//            slider.addEventListener('input', function () {
//                const value = (this.value - this.min) / (this.max - this.min) * 100;
//                this.style.background = `linear-gradient(to right, #6366f1 0%, #6366f1 ${value}%, #333333 ${value}%, #333333 100%)`;
//            });

//            // Initialize slider
//            slider.dispatchEvent(new Event('input'));
//        });
//    }

//    /**
//     * Handle decrease button click
//     */
//    function handleDecrease() {
//        if (!imageCount) return;

//        const current = parseInt(imageCount.value);
//        if (current > MIN_COUNT) {
//            imageCount.value = current - 1;
//        }
//    }

//    /**
//     * Handle increase button click
//     */
//    function handleIncrease() {
//        if (!imageCount) return;

//        const current = parseInt(imageCount.value);
//        if (current < MAX_COUNT) {
//            imageCount.value = current + 1;
//        }
//    }

//    /**
//     * Handle generate button click
//     */
//    function handleGenerate() {
//        // Validate prompt using PromptHandler
//        if (typeof PromptHandler !== 'undefined' && !PromptHandler.validate()) {
//            return;
//        }

//        // Form will handle the actual submission
//        // This is just for additional validation
//    }

//    /**
//     * Handle clear all button click
//     */
//    function handleClearAll() {
//        // Reset prompt
//        if (typeof PromptHandler !== 'undefined') {
//            PromptHandler.clear();
//        }

//        // Reset image count
//        if (imageCount) {
//            imageCount.value = '1';
//        }

//        // Reset face lock
//        if (typeof FaceLockPanel !== 'undefined') {
//            FaceLockPanel.reset();
//        }

//        // Reset flux styles
//        if (typeof FluxImageStyles !== 'undefined') {
//            FluxImageStyles.reset();
//        }

//        // Reset model selection
//        if (typeof ModelSelectionPanel !== 'undefined') {
//            ModelSelectionPanel.reset();
//        }

//        // Reset to default state
//        showDefaultState();
//    }

//    /**
//     * Show default state
//     */
//    function showDefaultState() {
//        if (loadingState) loadingState.classList.add('hidden');
//        if (imageContainer) imageContainer.classList.add('hidden');
//        if (defaultState) defaultState.classList.remove('hidden');
//    }

//    /**
//     * Show loading state
//     */
//    function showLoadingState() {
//        if (defaultState) defaultState.classList.add('hidden');
//        if (imageContainer) imageContainer.classList.add('hidden');
//        if (loadingState) loadingState.classList.remove('hidden');
//    }

//    /**
//     * Show generated image
//     * @param {string} imageUrl
//     */
//    function showGeneratedImage(imageUrl) {
//        if (defaultState) defaultState.classList.add('hidden');
//        if (loadingState) loadingState.classList.add('hidden');
//        if (imageContainer) imageContainer.classList.remove('hidden');
//        if (generatedImage) generatedImage.src = imageUrl;
//    }

//    /**
//     * Get current image count
//     * @returns {number}
//     */
//    function getImageCount() {
//        return imageCount ? parseInt(imageCount.value) : 1;
//    }

//    /**
//     * Set image count
//     * @param {number} count
//     */
//    function setImageCount(count) {
//        if (imageCount) {
//            imageCount.value = Math.max(MIN_COUNT, Math.min(MAX_COUNT, count));
//        }
//    }

//    // Public API
//    return {
//        init,
//        showDefaultState,
//        showLoadingState,
//        showGeneratedImage,
//        getImageCount,
//        setImageCount
//    };
//})();

//// Export for module systems
//if (typeof module !== 'undefined' && module.exports) {
//    module.exports = ImageControls;
//}









/**
 * Image Controls Module
 * Handles image count, sliders, and canvas state management
 * 
 * ⚠️ Bu modül UI DURUMLARINI yönetir.
 *    Form submit işlemi app.js'te yapılır.
 *    generateBtn click event'i KALDIRILDI (form submit ile çakışıyordu)
 */

const ImageControls = (function () {
    'use strict';

    // DOM Elements
    let decreaseCount = null;
    let increaseCount = null;
    let imageCount = null;
    let generateBtn = null;
    let clearAllBtn = null;
    let defaultState = null;
    let loadingState = null;
    let imageContainer = null;
    let generatedImage = null;

    // Configuration
    const MIN_COUNT = 1;
    const MAX_COUNT = 4;

    /**
     * Initialize image controls
     */
    function init() {
        cacheElements();
        bindEvents();
        initializeSliders();
    }

    /**
     * Cache DOM elements
     */
    function cacheElements() {
        decreaseCount = document.getElementById('decreaseCount');
        increaseCount = document.getElementById('increaseCount');
        imageCount = document.getElementById('imageCount');
        generateBtn = document.getElementById('generateBtn');
        clearAllBtn = document.getElementById('clearAllBtn');
        defaultState = document.getElementById('defaultState');
        loadingState = document.getElementById('loadingState');
        imageContainer = document.getElementById('imageContainer');
        generatedImage = document.getElementById('generatedImage');
    }

    /**
     * Bind event listeners
     * 
     * ⚠️ generateBtn click event KALDIRILDI!
     * NEDEN? 
     *   generateBtn type="submit" → form submit event'i tetikler
     *   app.js form submit'i yakalar (e.preventDefault + apiFetch)
     *   Eğer burada da click event olursa ÇAKİŞİR.
     */
    function bindEvents() {
        if (decreaseCount) {
            decreaseCount.addEventListener('click', handleDecrease);
        }

        if (increaseCount) {
            increaseCount.addEventListener('click', handleIncrease);
        }

        // ❌ ESKİ: generateBtn click event (KALDIRILDI - app.js yönetiyor)
        // if (generateBtn) {
        //     generateBtn.addEventListener('click', handleGenerate);
        // }

        if (clearAllBtn) {
            clearAllBtn.addEventListener('click', handleClearAll);
        }
    }

    /**
     * Initialize all sliders
     */
    function initializeSliders() {
        const sliders = document.querySelectorAll('input[type="range"]');
        sliders.forEach(slider => {
            slider.addEventListener('input', function () {
                const value = (this.value - this.min) / (this.max - this.min) * 100;
                this.style.background = `linear-gradient(to right, #6366f1 0%, #6366f1 ${value}%, #333333 ${value}%, #333333 100%)`;
            });

            // Initialize slider
            slider.dispatchEvent(new Event('input'));
        });
    }

    /**
     * Handle decrease button click
     */
    function handleDecrease() {
        if (!imageCount) return;

        const current = parseInt(imageCount.value);
        if (current > MIN_COUNT) {
            imageCount.value = current - 1;
        }
    }

    /**
     * Handle increase button click
     */
    function handleIncrease() {
        if (!imageCount) return;

        const current = parseInt(imageCount.value);
        if (current < MAX_COUNT) {
            imageCount.value = current + 1;
        }
    }

    // ❌ ESKİ: handleGenerate KALDIRILDI
    // Form submit artık app.js tarafından yönetiliyor

    /**
     * Handle clear all button click
     */
    function handleClearAll() {
        if (typeof PromptHandler !== 'undefined') {
            PromptHandler.clear();
        }

        if (imageCount) {
            imageCount.value = '1';
        }

        if (typeof FaceLockPanel !== 'undefined') {
            FaceLockPanel.reset();
        }

        if (typeof FluxImageStyles !== 'undefined') {
            FluxImageStyles.reset();
        }

        if (typeof ModelSelectionPanel !== 'undefined') {
            ModelSelectionPanel.reset();
        }

        showDefaultState();

        // 🆕 Clear All yapıldığında bilgilendir
        Toast.info('Tüm alanlar temizlendi.', 'Sıfırlandı');
    }

    // ═══════════════════════════════════════
    // CANVAS DURUM YÖNETİMİ
    // Bu fonksiyonlar app.js tarafından çağrılır
    // ═══════════════════════════════════════

    /**
     * Show default state (boş canvas)
     */
    function showDefaultState() {
        if (loadingState) loadingState.classList.add('hidden');
        if (imageContainer) imageContainer.classList.add('hidden');
        if (defaultState) defaultState.classList.remove('hidden');
    }

    /**
     * Show loading state (spinner)
     */
    function showLoadingState() {
        if (defaultState) defaultState.classList.add('hidden');
        if (imageContainer) imageContainer.classList.add('hidden');
        if (loadingState) loadingState.classList.remove('hidden');
    }

    /**
     * Show generated image
     * @param {string} imageUrl
     */
    function showGeneratedImage(imageUrl) {
        if (defaultState) defaultState.classList.add('hidden');
        if (loadingState) loadingState.classList.add('hidden');
        if (imageContainer) imageContainer.classList.remove('hidden');
        if (generatedImage) generatedImage.src = imageUrl;
    }

    /**
     * 🆕 Generate butonunun durumunu değiştir
     * @param {boolean} isLoading
     */
    function setGenerateButtonState(isLoading) {
        if (!generateBtn) return;

        if (isLoading) {
            generateBtn.disabled = true;
            generateBtn.innerHTML =
                '<i class="fas fa-spinner fa-spin mr-2"></i>Generating...';
        } else {
            generateBtn.disabled = false;
            generateBtn.innerHTML =
                '<i class="fas fa-magic mr-2"></i>Generate Image';
        }
    }

    /**
     * Get current image count
     * @returns {number}
     */
    function getImageCount() {
        return imageCount ? parseInt(imageCount.value) : 1;
    }

    /**
     * Set image count
     * @param {number} count
     */
    function setImageCount(count) {
        if (imageCount) {
            imageCount.value = Math.max(MIN_COUNT, Math.min(MAX_COUNT, count));
        }
    }

    // Public API
    return {
        init,
        showDefaultState,
        showLoadingState,
        showGeneratedImage,
        setGenerateButtonState,  // 🆕
        getImageCount,
        setImageCount
    };
})();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = ImageControls;
}