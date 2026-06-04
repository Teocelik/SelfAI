const auth = window.firebaseAuth;

// returnUrl'i (varsa) e-posta link'inin continue URL'ine ekle.
// Firebase, link'e kendi parametrelerini eklerken mevcut query string'i korur,
// böylece callback sayfasında returnUrl okunabilir. Sadece site içi göreli yollara izin verilir.
const _linkParams = new URLSearchParams(window.location.search);
const _rawReturnUrl = _linkParams.get('returnUrl');
const _safeReturnUrl = (_rawReturnUrl && _rawReturnUrl.startsWith('/') && !_rawReturnUrl.startsWith('//'))
    ? _rawReturnUrl
    : null;
const _callbackUrl = "https://localhost:44305/Account/EmailLoginCallback"
    + (_safeReturnUrl ? ("?returnUrl=" + encodeURIComponent(_safeReturnUrl)) : "");

const actionCodeSettings = {
    url: _callbackUrl,
    handleCodeInApp: true
};

document.getElementById("emailLoginForm").addEventListener("submit", function (e) {
    e.preventDefault();
    const email = document.getElementById("userEmail").value;

    auth.sendSignInLinkToEmail(email, actionCodeSettings)
        .then(() => {
            alert("Giriş bağlantısı gönderildi.:)");
            window.localStorage.setItem('emailForSignIn', email);
        })
        .catch((error) => {
            console.error("Hata:", error);
            alert("Gönderim başarısız: " + error.message);
        });
});
