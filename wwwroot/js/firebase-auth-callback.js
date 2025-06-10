// Firebase Auth. callback script to handle email link sign-in
const auth = window.firebaseAuth;

if (auth.isSignInWithEmailLink(window.location.href)) {
    let email = window.localStorage.getItem('emailForSignIn');
    if (!email) {
        email = prompt('E-posta adresinizi tekrar girin:');
    }

    auth.signInWithEmailLink(email, window.location.href)
        .then((result) => {
            alert("Giriş başarılı!");
            window.localStorage.removeItem('emailForSignIn');
            return result.user.getIdToken();
        })
        .then((token) => {
            console.log("JWT Token:", token);
            window.location.href = "/Home/Index";
        })
        .catch((error) => {
            console.error("Giriş hatası:", error);
            alert("Giriş başarısız: " + error.message);
        });
}
