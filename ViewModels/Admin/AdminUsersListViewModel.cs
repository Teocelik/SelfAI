using SelfAI.DTOs.Admin;

namespace SelfAI.ViewModels.Admin
{
    /// <summary>
    /// F.9a — Paginated kullanıcı listesi + arama/tarih filtresi.
    /// F.7.3 AdminUsersViewModel (tekil arama kartı) ile karıştırılmamalı — bu ayrı bir sistem.
    /// </summary>
    public class AdminUsersListViewModel
    {
        public AdminUserListDto? UserList { get; set; }
        public string? SearchTerm { get; set; }
        public string? DateFilter { get; set; }   // "all", "7d", "30d"
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
