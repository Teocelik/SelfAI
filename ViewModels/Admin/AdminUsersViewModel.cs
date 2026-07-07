using SelfAI.DTOs.Admin;

namespace SelfAI.ViewModels.Admin
{
    public class AdminUsersViewModel
    {
        public string? SearchEmail { get; set; }
        public AdminUserDto? FoundUser { get; set; }
        public string? NotFoundMessage { get; set; }
    }
}
