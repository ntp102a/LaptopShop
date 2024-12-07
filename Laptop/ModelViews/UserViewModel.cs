namespace LaptopShop.ModelViews
{
    public class UserViewModel
    {
        public string UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Salt { get; set; }
        public int? RoleId { get; set; }
        public bool? IsVerified { get; set; }
    }
}
