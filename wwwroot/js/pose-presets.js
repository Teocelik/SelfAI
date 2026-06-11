/**
 * Pose Presets Module
 * Pose Lock panelindeki "Hızlı Şablonlar" preset kartlarını yönetir.
 * Bir karta tıklanınca ilgili görsel, pose-lock-panel.js'in expose ettiği
 * window.PoseLockPanel.uploadFromUrl API'si ile mevcut upload akışına sokulur
 * (görsel → /RenderNet/GetAssetId → asset_id → hidden input + buton thumbnail).
 *
 * Backend değişikliği YOK. Görsel yoksa broken-image handler placeholder gösterir.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const presets = document.querySelectorAll('.pose-preset');
        presets.forEach(function (preset) {
            preset.addEventListener('click', handlePresetClick);
        });

        // Broken image handler — görsel yoksa placeholder göster
        const images = document.querySelectorAll('.pose-preset__image img');
        images.forEach(function (img) {
            img.addEventListener('error', function () {
                img.classList.add('is-broken');
            });
        });
    });

    async function handlePresetClick(event) {
        const btn = event.currentTarget;
        const presetUrl = btn.dataset.presetUrl;
        const presetName = btn.dataset.presetName;

        if (!presetUrl) return;

        // Loading state
        btn.classList.add('is-loading');

        // Diğer presetlerin selected state'ini temizle
        document.querySelectorAll('.pose-preset.is-selected').forEach(function (el) {
            el.classList.remove('is-selected');
        });

        try {
            // PoseLockPanel API'sini çağır (pose-lock-panel.js'ten expose edildi)
            if (!window.PoseLockPanel || !window.PoseLockPanel.uploadFromUrl) {
                throw new Error('PoseLockPanel modülü hazır değil.');
            }

            const result = await window.PoseLockPanel.uploadFromUrl(presetUrl, presetName);

            if (result.success) {
                btn.classList.add('is-selected');
                if (typeof Toast !== 'undefined' && Toast.success) {
                    Toast.success('"' + presetName + '" pozu seçildi.');
                }
            } else {
                if (typeof Toast !== 'undefined' && Toast.error) {
                    Toast.error('Preset yüklenemedi: ' + (result.error || 'Bilinmeyen hata'));
                }
            }
        } catch (error) {
            console.error('Preset click hatası:', error);
            if (typeof Toast !== 'undefined' && Toast.error) {
                Toast.error('Preset yüklenirken hata oluştu.');
            }
        } finally {
            btn.classList.remove('is-loading');
        }
    }
})();
