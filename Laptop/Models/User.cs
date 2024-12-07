using System;
using System.Collections.Generic;

namespace LaptopShop.Models
{
    public partial class User
    {
        public User()
        {
            Carts = new HashSet<Cart>();
            Orders = new HashSet<Order>();
            Reviews = new HashSet<Review>();
        }

        public string UserId { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public string? Salt { get; set; }
        public bool IsVerified { get; set; } = false;

        public virtual Role? Role { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}
