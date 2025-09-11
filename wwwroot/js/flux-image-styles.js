//document.addEventListener("DOMContentLoaded", () => {
//    const btn = document.getElementById("fluxButton");
//    if (!btn) return;

//    btn.addEventListener("click", async () => {
//        try {
//            const response = await fetch("/RenderNet/GetStyles");
//            if (!response.ok) throw new Error("Hata: " + response.status);

//            const data = await response.json();
//            console.log("Stiller:", data);

//            const container = document.getElementById("stylesContainer");
//            container.innerHTML = "";

//            data.forEach(style => {
//                const div = document.createElement("div");
//                div.className = "style-box";
//                div.innerHTML = `
//                    <img src="${style.thumbnail_url}" alt="${style.name}" />
//                    <p>${style.name}</p>`;
//                container.appendChild(div);
//            });
//        } catch (err) {
//            console.error(err);
//            alert("Stiller yüklenemedi.");
//        }
//    });
//});
