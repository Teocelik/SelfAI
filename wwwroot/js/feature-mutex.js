/**
 * Feature Mutex Module (F.M.6)
 *
 * Character / Face Lock kişiselleştirmeleri karşılıklı dışlamalıdır: her biri
 * generation endpoint'ini override ettiği için (LoRA / PuLID) aynı anda yalnızca
 * biri aktif olabilir. Bir feature aktifleştiğinde diğeri temizlenir ve
 * 'feature-mutex-changed' event'i yayılır.
 *
 * Override rozeti render'ı model-picker.js'e taşındı (bu event'i dinler).
 *
 * Mevcut panel public API'lerine map'lenir:
 *   - CharacterPanel.clearCharacter() / isCharacterActive()
 *   - FaceLockPanel.reset()           / getFaceAssetId()
 */

const FeatureMutex = (function () {
    'use strict';

    /**
     * Aktif olan feature'ı belirler, diğerini temizler ve rozet event'i yayar.
     * @param {('character'|'face'|null)} active
     */
    function setActive(active) {
        if (active !== 'character'
            && window.CharacterPanel
            && typeof window.CharacterPanel.clearCharacter === 'function') {
            window.CharacterPanel.clearCharacter();
        }
        if (active !== 'face'
            && window.FaceLockPanel
            && typeof window.FaceLockPanel.reset === 'function') {
            window.FaceLockPanel.reset();
        }

        // UI feedback — model-picker.js override rozeti dinler
        document.dispatchEvent(new CustomEvent('feature-mutex-changed', {
            detail: { active: active || null }
        }));
    }

    /**
     * Şu an aktif olan feature'ı döndürür (panellerin canlı state'inden).
     * @returns {('character'|'face'|null)}
     */
    function getActive() {
        if (window.CharacterPanel
            && typeof window.CharacterPanel.isCharacterActive === 'function'
            && window.CharacterPanel.isCharacterActive()) {
            return 'character';
        }
        if (window.FaceLockPanel
            && typeof window.FaceLockPanel.getFaceAssetId === 'function'
            && window.FaceLockPanel.getFaceAssetId()) {
            return 'face';
        }
        return null;
    }

    return { setActive, getActive };
})();

// Diğer modüller window.FeatureMutex üzerinden erişir
if (typeof window !== 'undefined') {
    window.FeatureMutex = FeatureMutex;
}

// Modül sistemleri için export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FeatureMutex;
}
