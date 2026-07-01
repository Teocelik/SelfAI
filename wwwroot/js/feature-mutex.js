/**
 * Feature Mutex Module (F.M.6)
 *
 * Character / Face Lock / Pose Lock kişiselleştirmeleri karşılıklı dışlamalıdır:
 * her biri generation endpoint'ini override ettiği için (LoRA / PuLID / ControlNet)
 * aynı anda yalnızca biri aktif olabilir. Bir feature aktifleştiğinde diğer ikisi
 * temizlenir ve 'feature-mutex-changed' event'i yayılır.
 *
 * F.M.6 hotfix: Pose Lock UI geçici gizli (PoseLockPanel.getPoseImageUrl her zaman
 * null döner), bu yüzden pratikte yalnızca Character ↔ Face mutex'i aktif çalışır.
 * Override rozeti render'ı model-picker.js'e taşındı (bu event'i dinler).
 *
 * Mevcut panel public API'lerine map'lenir:
 *   - CharacterPanel.clearCharacter() / isCharacterActive()
 *   - FaceLockPanel.reset()           / getFaceImageUrl()
 *   - PoseLockPanel.reset()           / getPoseImageUrl()
 */

const FeatureMutex = (function () {
    'use strict';

    /**
     * Aktif olan feature'ı belirler, diğerlerini temizler ve rozet event'i yayar.
     * @param {('character'|'face'|'pose'|null)} active
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
        if (active !== 'pose'
            && window.PoseLockPanel
            && typeof window.PoseLockPanel.reset === 'function') {
            window.PoseLockPanel.reset();
        }

        // UI feedback — model-picker.js override rozeti dinler
        document.dispatchEvent(new CustomEvent('feature-mutex-changed', {
            detail: { active: active || null }
        }));
    }

    /**
     * Şu an aktif olan feature'ı döndürür (panellerin canlı state'inden).
     * @returns {('character'|'face'|'pose'|null)}
     */
    function getActive() {
        if (window.CharacterPanel
            && typeof window.CharacterPanel.isCharacterActive === 'function'
            && window.CharacterPanel.isCharacterActive()) {
            return 'character';
        }
        if (window.FaceLockPanel
            && typeof window.FaceLockPanel.getFaceImageUrl === 'function'
            && window.FaceLockPanel.getFaceImageUrl()) {
            return 'face';
        }
        if (window.PoseLockPanel
            && typeof window.PoseLockPanel.getPoseImageUrl === 'function'
            && window.PoseLockPanel.getPoseImageUrl()) {
            return 'pose';
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
