using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace LaptopShop.ModelViews
{
    public class ChangeInfoViewModel
    {
        [RegularExpression(@"^[^\d!@#$%^&*()_+]+(?: [^\d!@#$%^&*()_+]+)*$", ErrorMessage = "Tên không được sử dụng số")]
        public string FullName { get; set; }
        [RegularExpression(@"^[^!@#$%^&*()+=[\]{};:'""|<>?`~]*$", ErrorMessage = "Địa chỉ không hợp lệ")]
        public string Address { get; set; }
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

    }
}
