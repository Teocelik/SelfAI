namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetGenerationService
    {
        Task<string> GenerateMediaAsync(string assetId);
    }
}
