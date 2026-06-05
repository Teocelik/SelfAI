using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface ICreditService
    {
        // Mevcut bakiyeyi getirir
        Task<int> GetBalanceAsync(Guid userId);

        // Atomik düşüm. Yetersiz bakiye → ServiceResult.Failure(402)
        Task<ServiceResult<int>> TryDeductAsync(Guid userId, int amount, string description);

        // Geri iade. relatedGenerationId opsiyonel (audit için)
        Task<ServiceResult<int>> RefundAsync(Guid userId, int amount, string reason, Guid? relatedGenerationId = null);
    }
}
