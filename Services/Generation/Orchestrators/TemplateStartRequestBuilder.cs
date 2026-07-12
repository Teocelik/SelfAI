using SelfAI.DTOs.Templates;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Templates (F.M.10a) için format → <see cref="StartGenerationRequest"/> çeviren
/// hafif static builder. Ayrı bir orchestrator YOK — kredi/iade/log/SignalR mantığı
/// tekil olarak <see cref="GenerationOrchestrator"/>'da yaşar (DRY). Templates'in tek
/// katkısı, format'ın endpoint + aspect ratio + kompozisyon suffix'ini mevcut generation
/// isteğine map'lemektir.
/// </summary>
public static class TemplateStartRequestBuilder
{
    /// <summary>
    /// Seçili format + kullanıcı isteğinden generation isteği kurar. Kompozisyon suffix'i
    /// opsiyonel (transparent — kullanıcı kapatabilir).
    ///
    /// F.M.10b: <see cref="TemplateGenerationRequest.CharacterId"/> dolu ise sadece taşınır —
    /// endpoint override'ını (flux-lora) ve LoRA URL resolve'unu <c>GenerationOrchestrator</c>
    /// CharacterId'den kendisi yapar (builder ModelEndpoint'e dokunmaz). CharacterId null ise
    /// davranış F.M.10a ile birebir aynıdır.
    /// </summary>
    public static StartGenerationRequest Build(PostFormatDto format, TemplateGenerationRequest request)
    {
        var prompt = request.Prompt?.Trim() ?? string.Empty;
        var finalPrompt = request.UsePromptSuffix
            ? $"{prompt}{format.PromptSuffix}"
            : prompt;

        return new StartGenerationRequest
        {
            ModelEndpoint = format.ModelEndpoint,
            Prompt = finalPrompt,
            AspectRatio = format.AspectRatioValue,
            NumImages = 1,
            CharacterId = request.CharacterId
        };
    }
}
