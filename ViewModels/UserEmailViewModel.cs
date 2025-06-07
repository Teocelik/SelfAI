using System.ComponentModel.DataAnnotations;

namespace SelfAI.ViewModels
{
    public class UserEmailViewModel
    {
        [Required]
        public string Email { get; private set; }
    }
}
