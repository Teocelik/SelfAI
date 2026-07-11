using SelfAI.DTOs.Admin;

namespace SelfAI.ViewModels.Admin
{
    /// <summary>
    /// F.9a — Kullanıcı detay + işlem geçmişi (tek sayfa).
    /// </summary>
    public class AdminUserDetailViewModel
    {
        public AdminUserDetailDto? User { get; set; }
        public AdminTransactionListDto? Transactions { get; set; }
        public int TransactionPage { get; set; } = 1;
    }
}
