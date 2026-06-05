using SelfAI.Entities;

namespace SelfAI.Services.Interfaces
{
    public interface IPackageService
    {
        Task<IReadOnlyList<Package>> GetActivePackagesAsync();
    }
}
