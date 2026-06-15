/**
 * Pose Presets Module (F.5b — Modal Masonry Şablonlar)
 * Pose Lock modal'ındaki masonry şablon kartlarını (.pose-template) yönetir.
 * Bir karta tıklanınca ilgili görsel, pose-lock-panel.js'in expose ettiği
 * window.PoseLockPanel.uploadFromUrl API'si ile mevcut upload akışına sokulur
 * (görsel → /RenderNet/GetAssetId → asset_id → hidden input + buton thumbnail).
 *
 * Backend değişikliği YOK. Var olmayan görseller broken-image handler ile
 * TAMAMEN GİZLENİR (sadece gerçek şablonlar görünür — Affogato davranışı).
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const templates = document.querySelectorAll('.pose-template');
        templates.forEach(function (template) {
            template.addEventListener('click', handleTemplateClick);
        });

        // Broken image handler — görsel yoksa kartı tamamen gizle
        const images = document.querySelectorAll('.pose-template img');
        images.forEach(function (img) {
            img.addEventListener('error', function () {
                const template = img.closest('.pose-template');
                if (template) {
                    template.classList.add('is-hidden');
                }
            });
        });
    });

    async function handleTemplateClick(event) {
        const btn = event.currentTarget;
        const presetUrl = btn.dataset.presetUrl;
        const presetName = btn.dataset.presetName;

        if (!presetUrl) return;

        // Loading state
        btn.classList.add('is-loading');

        // Diğer şablonların selected state'ini temizle
        document.querySelectorAll('.pose-template.is-selected').forEach(function (el) {
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
                // Kullanıcı seçim yaptı → modal'ı kapat
                if (window.PoseLockPanel && typeof window.PoseLockPanel.close === 'function') {
                    window.PoseLockPanel.close();
                }
            } else {
                if (typeof Toast !== 'undefined' && Toast.error) {
                    Toast.error('Şablon yüklenemedi: ' + (result.error || 'Bilinmeyen hata'));
                }
            }
        } catch (error) {
            console.error('Şablon click hatası:', error);
            if (typeof Toast !== 'undefined' && Toast.error) {
                Toast.error('Şablon yüklenirken hata oluştu.');
            }
        } finally {
            btn.classList.remove('is-loading');
        }
    }
})();
