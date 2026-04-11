using System.ComponentModel.DataAnnotations;

namespace WebSapaFreshWay.Models
{
    public class OtpRequestDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;
    }
}
