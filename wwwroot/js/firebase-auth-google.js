
const provider = new firebase.auth.GoogleAuthProvider();

document.getElementById("googleLoginBtn").addEventListener("click", function () {
    auth.signInWithPopup(provider)
        .then((result) => {
            const user = result.user;
            return user.getIdToken();
        })
        .then((token) => {
            console.log("Google JWT Token:", token);
            window.location.href = "/Home/Index";
        })
        .catch((error) => {
            console.error("Google Giriş Hatası:", error);
            alert("Giriş başarısız: " + error.message);
        });
});
