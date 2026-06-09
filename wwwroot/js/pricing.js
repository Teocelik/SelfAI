/* ═══════════════════════════════════════════════════════════════
   Pricing — Abone Ol akışı (D.3.2 davranışı korunur).
   Inline <script>'tan taşındı (MVC separation — CLAUDE.md kuralı).
   IIFE modül kalıbı (mevcut konvansiyon).
   ═══════════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const subscribeButtons = document.querySelectorAll('.subscribe-btn');

        subscribeButtons.forEach(function (btn) {
            btn.addEventListener('click', handleSubscribeClick);
        });
    });

    async function handleSubscribeClick(event) {
        const btn = event.currentTarget;
        const packageId = btn.dataset.packageId;

        if (!packageId || packageId === '0') {
            return;
        }

        const originalText = btn.textContent;
        btn.disabled = true;
        btn.textContent = 'Yönlendiriliyor...';

        try {
            const response = await fetch('/Pricing/Subscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ packageId: parseInt(packageId) })
            });

            if (response.status === 401) {
                const returnUrl = encodeURIComponent('/Pricing');
                window.location.href = `/Account/Login?returnUrl=${returnUrl}`;
                return;
            }

            const data = await response.json();

            if (response.ok && data.paymentPageUrl) {
                window.location.href = data.paymentPageUrl;
            } else {
                showError(data.message || 'Bir hata oluştu.');
                btn.disabled = false;
                btn.textContent = originalText;
            }
        } catch (err) {
            console.error('Subscribe hatası:', err);
            showError('Bağlantı hatası, lütfen tekrar deneyin.');
            btn.disabled = false;
            btn.textContent = originalText;
        }
    }

    function showError(message) {
        if (typeof Toast !== 'undefined' && typeof Toast.error === 'function') {
            Toast.error(message);
        } else {
            alert(message);
        }
    }
})();
