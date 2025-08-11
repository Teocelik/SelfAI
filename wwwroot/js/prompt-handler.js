document.addEventListener("DOMContentLoaded", () => {
    const promptInput = document.getElementById("promptInput");
    const randomPromptButton = document.getElementById("randomPromptButton");

    function getRandomPrompt() {
        const randomIndex = Math.floor(Math.random() * prompts.length);
        return prompts[randomIndex];
    }

    randomPromptButton.addEventListener("click", () => {
        const randomPrompt = getRandomPrompt();
        promptInput.value = randomPrompt;
    });
});
