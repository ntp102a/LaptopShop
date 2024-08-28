using System.ComponentModel.DataAnnotations;

namespace LaptopShop.ModelViews
{
    public class LoginViewModel
    {
        [Key]
        [MaxLength(100)]
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [MinLength(5, ErrorMessage = " Bạn cần nhập mật khẩu tối thiểu 5 ký tự")]
        public string Password { get; set; }
    }
}
