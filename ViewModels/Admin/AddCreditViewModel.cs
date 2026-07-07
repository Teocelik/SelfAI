using System.ComponentModel.DataAnnotations;

namespace SelfAI.ViewModels.Admin
{
    public class AddCreditViewModel
    {
        public Guid TargetUserId { get; set; }

        public string TargetEmail { get; set; } = string.Empty;

        public int CurrentBalance { get; set; }

        [Required(ErrorMessage = "Miktar gerekli.")]
        [Range(1, 10000, ErrorMessage = "Miktar 1 ile 10000 arasında olmalı.")]
        public int Amount { get; set; }

        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        public string? Note { get; set; }
    }
}
