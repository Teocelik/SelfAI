using System.ComponentModel.DataAnnotations;

namespace SelfAI.ViewModels
{
    public class UserEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; private set; }
    }
}
