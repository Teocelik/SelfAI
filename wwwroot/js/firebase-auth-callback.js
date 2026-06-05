// Firebase Auth. callback script to handle email link sign-in
const auth = window.firebaseAuth;

if (auth.isSignInWithEmailLink(window.location.href)) {
    let email = window.localStorage.getItem('emailForSignIn');
    if (!email) {
        email = prompt('E-posta adresinizi tekrar girin:');
    }

    auth.signInWithEmailLink(email, window.location.href)
        .then((result) => {
            window.localStorage.removeItem('emailForSignIn');
            return result.user.getIdToken();
        })
        .then((token) => {
            return fetch('/Account/VerifyToken', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ idToken: token })
            });
        })
        .then((response) => {
            if (response.ok) {
                // Giriş sonrası returnUrl'e dön (yoksa /Home/Index).
                // returnUrl, e-posta link'inin continue URL'inden (firebase-auth-link.js) gelir.
                // Açık yönlendirme koruması: yalnızca site içi göreli yollara izin ver.
                const params = new URLSearchParams(window.location.search);
                const raw = params.get('returnUrl');
                const returnUrl = (raw && raw.startsWith('/') && !raw.startsWith('//')) ? raw : '/RenderNet/Index';
                window.location.href = returnUrl;
            } else {
                return response.json().then(err => {
                    throw new Error(err.message || 'Sunucu doğrulaması başarısız.');
                });
            }
        })
        .catch((error) => {
            console.error("Giriş hatası:", error);
            document.getElementById('emailLoginStatus').innerHTML =
                '<i class="fas fa-exclamation-circle text-4xl text-red-500 mb-4"></i>' +
                '<p class="text-white text-lg">Giriş başarısız: ' + error.message + '</p>';
        });
}
