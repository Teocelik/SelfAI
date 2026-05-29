const PromptHandler = (function () {
    'use strict';

    // Backend'den yüklenen hazır prompt listesi
    let randomPrompts = [];

    // DOM Elements
    let promptInput = null;
    let charCount = null;
    let randomPromptBtn = null;

    // Configuration
    const MAX_CHARS = 500;

    /**
     * Initialize the prompt handler
     */
    function init() {
        cacheElements();
        bindEvents();
        loadPromptsFromBackend();
    }

    /**
     * Hazır prompt listesini backend'den yükler.
     * apiFetch hata durumunda kendi Toast.error'ını gösterip null döndürür,
     * bu yüzden burada ekstra toast göstermiyoruz; sadece console.error log'larız.
     */
    async function loadPromptsFromBackend() {
        const response = await apiFetch('/Prompt/GetAll');

        if (!response || !Array.isArray(response.data)) {
            console.error('Promptlar yüklenemedi:', response);
            return;
        }

        randomPrompts = response.data;
    }

    /**
     * Cache DOM elements
     */
    function cacheElements() {
        promptInput = document.getElementById('promptInput');
        charCount = document.getElementById('charCount');
        randomPromptBtn = document.getElementById('randomPromptBtn');
    }

    /**
     * Bind event listeners
     */
    function bindEvents() {
        if (promptInput) {
            promptInput.addEventListener('input', handlePromptInput);
        }

        if (randomPromptBtn) {
            randomPromptBtn.addEventListener('click', handleRandomPrompt);
        }
    }

    /**
     * Handle prompt input changes
     */
    function handlePromptInput() {
        const count = promptInput.value.length;

        if (charCount) {
            charCount.textContent = `${count}/${MAX_CHARS}`;

            if (count > MAX_CHARS) {
                charCount.classList.add('text-error');
            } else {
                charCount.classList.remove('text-error');
            }
        }
    }

    /**
     * Handle random prompt button click
     */
    function handleRandomPrompt() {
        if (!randomPromptBtn || !promptInput) return;

        // Add loading state
        const originalHTML = randomPromptBtn.innerHTML;
        randomPromptBtn.innerHTML = '<i class="fas fa-spinner fa-spin text-text-secondary text-sm"></i>';
        randomPromptBtn.disabled = true;

        // Simulate brief loading for better UX
        setTimeout(() => {
            try {
                if (randomPrompts.length === 0) {
                    Toast.warning('Prompt listesi henüz hazır değil.', 'Bekleyin');
                    return;
                }

                // Get random prompt from predefined array
                const randomIndex = Math.floor(Math.random() * randomPrompts.length);
                const randomPrompt = randomPrompts[randomIndex];

                promptInput.value = randomPrompt;

                // Trigger input event to update character count
                promptInput.dispatchEvent(new Event('input'));

                // Add visual feedback
                promptInput.classList.add('border-primary');
                setTimeout(() => {
                    promptInput.classList.remove('border-primary');
                }, 1000);

            } catch (error) {
                console.error('Error generating random prompt:', error);
                // Show error feedback
                randomPromptBtn.classList.add('border-error');
                setTimeout(() => {
                    randomPromptBtn.classList.remove('border-error');
                }, 1000);
            } finally {
                // Restore button state
                randomPromptBtn.innerHTML = originalHTML;
                randomPromptBtn.disabled = false;
            }
        }, 500);
    }

    /**
     * Get current prompt value
     * @returns {string}
     */
    function getPrompt() {
        return promptInput ? promptInput.value.trim() : '';
    }

    /**
     * Set prompt value
     * @param {string} value
     */
    function setPrompt(value) {
        if (promptInput) {
            promptInput.value = value;
            promptInput.dispatchEvent(new Event('input'));
        }
    }

    /**
     * Clear prompt
     */
    function clear() {
        if (promptInput) {
            promptInput.value = '';
            promptInput.dispatchEvent(new Event('input'));
        }
    }

    /**
     * Validate prompt
     * @returns {boolean}
     */
    function validate() {
        if (!promptInput) return false;

        if (promptInput.value.trim() === '') {
            promptInput.focus();
            promptInput.classList.add('border-error');
            setTimeout(() => {
                promptInput.classList.remove('border-error');
            }, 2000);
            return false;
        }
        return true;
    }

    // Public API
    return {
        init,
        getPrompt,
        setPrompt,
        clear,
        validate
    };
})();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PromptHandler;
}