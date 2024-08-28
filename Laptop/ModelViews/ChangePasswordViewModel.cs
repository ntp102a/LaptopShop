using System.ComponentModel.DataAnnotations;

namespace LaptopShop.ModelViews
{
    public class ChangePasswordViewModel
    {
        [Key]
        public int CustomerId { get; set; }

        [Display(Name = "Mật khẩu hiện tại")]
        public string PasswordNow { get; set; }

        [Display(Name = "Mật khẩu mới")]
        public string PasswordNew { get; set; }

        [Display(Name = "Nhập lại mật khẩu mới")]
        public string ConfirmPasswordNew { get; set; }
    }
}
