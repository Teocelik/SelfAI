namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetResourcesService
    {
        // RenderNet karakterlerini(stillerini) API'den çeker
        Task<string> ListFluxStyles();
    }
}
