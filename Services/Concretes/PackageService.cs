using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.Entities;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class PackageService : IPackageService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PackageService> _logger;

        public PackageService(AppDbContext db, ILogger<PackageService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Package>> GetActivePackagesAsync()
        {
            return await _db.Packages
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
        }
    }
}
