const auth = window.firebaseAuth;
const actionCodeSettings = {
    url: "https://localhost:44305/Account/EmailLoginCallback",
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
