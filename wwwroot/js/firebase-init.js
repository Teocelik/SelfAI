// Initialize Firebase
const firebaseConfig = {
    apiKey: "AIzaSyCs3jnlvwBKaho2orlurnA49OAbagSnEls",
    authDomain: "selfai-df09f.firebaseapp.com",
    projectId: "selfai-df09f",
    storageBucket: "selfai-df09f.firebasestorage.app",
    messagingSenderId: "968657508884",
    appId: "1:968657508884:web:a8e17c20d2e5bfd37aa501",
    measurementId: "G-BZFHJZZTJV"
};

firebase.initializeApp(firebaseConfig);
window.firebaseAuth = firebase.auth();
