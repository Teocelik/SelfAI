/**
 * Main Application Module
 * Coordinates all modules and initializes the application
 */

const App = (function () {
    'use strict';

    /**
     * Initialize all modules
     */
    function init() {
        // Initialize modules in correct order
        initPromptHandler();
        initFaceLockPanel();
        initFluxImageStyles();
        initModelSelectionPanel();
        initImageControls();

        console.log('AI Image Generation Studio initialized successfully');
    }

    /**
     * Initialize Prompt Handler
     */
    function initPromptHandler() {
        if (typeof PromptHandler !== 'undefined') {
            PromptHandler.init();
        } else {
            console.warn('PromptHandler module not found');
        }
    }

    /**
     * Initialize Face Lock Panel
     */
    function initFaceLockPanel() {
        if (typeof FaceLockPanel !== 'undefined') {
            FaceLockPanel.init();
        } else {
            console.warn('FaceLockPanel module not found');
        }
    }

    /**
     * Initialize Flux Image Styles
     */
    function initFluxImageStyles() {
        if (typeof FluxImageStyles !== 'undefined') {
            FluxImageStyles.init();
        } else {
            console.warn('FluxImageStyles module not found');
        }
    }

    /**
     * Initialize Model Selection Panel
     */
    function initModelSelectionPanel() {
        if (typeof ModelSelectionPanel !== 'undefined') {
            ModelSelectionPanel.init();
        } else {
            console.warn('ModelSelectionPanel module not found');
        }
    }

    /**
     * Initialize Image Controls
     */
    function initImageControls() {
        if (typeof ImageControls !== 'undefined') {
            ImageControls.init();
        } else {
            console.warn('ImageControls module not found');
        }
    }

    /**
     * Reset all modules
     */
    function resetAll() {
        if (typeof PromptHandler !== 'undefined') PromptHandler.clear();
        if (typeof FaceLockPanel !== 'undefined') FaceLockPanel.reset();
        if (typeof FluxImageStyles !== 'undefined') FluxImageStyles.reset();
        if (typeof ModelSelectionPanel !== 'undefined') ModelSelectionPanel.reset();
        if (typeof ImageControls !== 'undefined') ImageControls.showDefaultState();
    }

    // Public API
    return {
        init,
        resetAll
    };
})();

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    App.init();
});