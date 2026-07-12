using SelfAI.DTOs.Templates;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Hazır şablon (F.M.10b) için preset → <see cref="StartGenerationRequest"/> çeviren hafif
/// static builder. <see cref="TemplateStartRequestBuilder"/> ile aynı disiplin: ayrı orchestrator
/// YOK, mevcut <c>GenerationOrchestrator</c> pipeline'ı reuse edilir (kredi/iade/log/SignalR tekil).
///
/// Preset akışında kompozisyon suffix'i her zaman eklenir (F.M.10a'daki transparent toggle YOK —
/// suffix preset'in ayrılmaz parçası). Karakter seçilirse CharacterId taşınır; endpoint override'ını
/// orchestrator yapar (builder ModelEndpoint'e dokunmaz).
/// </summary>
public static class PresetStartRequestBuilder
{
    public static StartGenerationRequest Build(PresetTemplateDto preset, TemplateGenerationRequest request)
    {
        var prompt = request.Prompt?.Trim() ?? string.Empty;
        var finalPrompt = $"{prompt}{preset.ImagePromptSuffix}";

        return new StartGenerationRequest
        {
            ModelEndpoint = preset.ModelEndpoint,
            Prompt = finalPrompt,
            AspectRatio = preset.AspectRatioValue,
            NumImages = 1,
            CharacterId = request.CharacterId
        };
    }
}
