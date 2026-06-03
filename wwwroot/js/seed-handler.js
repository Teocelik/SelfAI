/**
 * Seed Handler Module
 * Seed (tohum) değerini yönetir: random / custom modları, dice butonu.
 *
 * ⚠️ Bu modül yalnızca FRONTEND state yönetir; network çağrısı YOK.
 *    - Random modda her Generate tıklamasında app.js refreshIfRandom() çağırır,
 *      böylece hidden Seed input yeni bir rastgele int ile güncellenir.
 *    - Custom modda dropdown altında number input görünür, kullanıcı değer girer.
 */

const SeedHandler = (function () {
    'use strict';

    // DOM Elements
    let seedModeSelect = null;
    let customSeedInput = null;
    let diceBtn = null;
    let seedHiddenInput = null;

    // Configuration
    const MAX_SEED = 2147483647;

    /**
     * Initialize the seed handler
     */
    function init() {
        cacheElements();
        bindEvents();

        // Dropdown default'u "random" olduğu için hidden input'u rastgele bir intle başlat
        generateRandomSeed();

        console.log('SeedHandler initialized');
    }

    /**
     * Cache DOM elements
     */
    function cacheElements() {
        seedModeSelect = document.getElementById('seedModeSelect');
        customSeedInput = document.getElementById('customSeedInput');
        diceBtn = document.getElementById('diceBtn');
        seedHiddenInput = document.getElementById('seedHiddenInput');
    }

    /**
     * Bind event listeners
     */
    function bindEvents() {
        if (seedModeSelect) {
            seedModeSelect.addEventListener('change', handleDropdownChange);
        }

        if (customSeedInput) {
            customSeedInput.addEventListener('input', handleCustomInputChange);
        }

        if (diceBtn) {
            diceBtn.addEventListener('click', handleDiceClick);
        }

        // Clear All butonu type="reset" → form reset event'ine bağlan
        const form = seedHiddenInput ? seedHiddenInput.closest('form') : null;
        if (form) {
            form.addEventListener('reset', handleFormReset);
        }
    }

    /**
     * Rastgele bir int üretir (1 ile MAX_SEED arası), hidden input'a yazar ve döndürür.
     * @returns {number}
     */
    function generateRandomSeed() {
        const seed = Math.floor(Math.random() * MAX_SEED) + 1;

        if (seedHiddenInput) {
            seedHiddenInput.value = seed;
        }

        return seed;
    }

    /**
     * Dropdown (random/custom) değişimini yönetir.
     */
    function handleDropdownChange() {
        if (!seedModeSelect) return;

        if (seedModeSelect.value === 'custom') {
            // Custom modu: number input'u göster
            if (customSeedInput) {
                customSeedInput.classList.remove('hidden');

                if (customSeedInput.value === '') {
                    // Input boşsa yeni random üret, hem custom'a hem hidden'a yaz
                    const seed = generateRandomSeed();
                    customSeedInput.value = seed;
                } else {
                    // Input doluysa hidden'ı custom değeriyle senkronla
                    if (seedHiddenInput) {
                        seedHiddenInput.value = customSeedInput.value;
                    }
                }
            }
        } else if (seedModeSelect.value === 'random') {
            // Random modu: number input'u gizle, yeni random üret (custom değerine dokunma)
            if (customSeedInput) {
                customSeedInput.classList.add('hidden');
            }
            generateRandomSeed();
        }
    }

    /**
     * Custom input değiştiğinde hidden input'u senkronlar.
     */
    function handleCustomInputChange() {
        // Yalnızca custom modda çalışsın
        if (!seedModeSelect || seedModeSelect.value !== 'custom') return;
        if (!customSeedInput || !seedHiddenInput) return;

        const value = parseInt(customSeedInput.value, 10);

        if (!isNaN(value) && value >= 0) {
            seedHiddenInput.value = value;
        } else {
            // Boş/NaN ise 0 (random) gönder
            seedHiddenInput.value = 0;
        }
    }

    /**
     * Dice butonu: modu custom'a çevirir, yeni random üretir, hem input'a hem hidden'a yazar.
     */
    function handleDiceClick() {
        if (seedModeSelect) {
            seedModeSelect.value = 'custom';
            // Input görünür olsun diye dropdown change mantığını manuel tetikle
            handleDropdownChange();
        }

        const seed = generateRandomSeed();
        if (customSeedInput) {
            customSeedInput.value = seed;
        }
    }

    /**
     * Form reset (Clear All) sonrası seed state'ini default'a döndürür.
     */
    function handleFormReset() {
        // Reset DOM güncellemesinden SONRA çalışsın
        setTimeout(function () {
            if (seedModeSelect) {
                seedModeSelect.value = 'random';
            }
            if (customSeedInput) {
                customSeedInput.value = '';
                customSeedInput.classList.add('hidden');
            }
            generateRandomSeed();
        }, 0);
    }

    /**
     * Public: Random modda yeni seed üretir, custom modda dokunmaz.
     * Üretilen değeri döndürür (custom modda undefined).
     * @returns {number|undefined}
     */
    function refreshIfRandom() {
        if (seedModeSelect && seedModeSelect.value === 'random') {
            return generateRandomSeed();
        }
        // Custom: kullanıcının değeri korunur
        return undefined;
    }

    // Public API
    return {
        init,
        refreshIfRandom
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SeedHandler;
}
