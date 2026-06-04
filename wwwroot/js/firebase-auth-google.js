
const provider = new firebase.auth.GoogleAuthProvider();

document.getElementById("googleLoginBtn").addEventListener("click", function () {
    firebase.auth().signInWithPopup(provider)
        .then((result) => {
            return result.user.getIdToken();
        })
        .then((token) => {
            // Backend'e token gönder
            return fetch('/Account/VerifyToken', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ idToken: token })
            });
        })
        .then((response) => {
            if (response.ok) {
                // Giriş sonrası returnUrl'e dön (yoksa /Home/Index).
                // Açık yönlendirme koruması: yalnızca site içi göreli yollara izin ver.
                const params = new URLSearchParams(window.location.search);
                const raw = params.get('returnUrl');
                const returnUrl = (raw && raw.startsWith('/') && !raw.startsWith('//')) ? raw : '/Home/Index';
                window.location.href = returnUrl;
            } else {
                return response.json().then(err => {
                    throw new Error(err.message || 'Sunucu doğrulaması başarısız.');
                });
            }
        })
        .catch((error) => {
            console.error("Google Giriş Hatası:", error);
            alert("Giriş başarısız: " + error.message);
        });
});
