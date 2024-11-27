using System;
using System.Collections.Generic;

namespace LaptopShop.Models
{
    public partial class Product
    {
        public Product()
        {
            Carts = new HashSet<Cart>();
            OrderDetails = new HashSet<OrderDetail>();
            Reviews = new HashSet<Review>();
        }

        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int? Price { get; set; }
        public int CategoryId { get; set; }
        public int? InfoId { get; set; }
        public int? Discount { get; set; }
        public int? Instock { get; set; }
        public int? ImageId { get; set; }

        public virtual Category? Category { get; set; }
        public virtual Image? Image { get; set; }
        public virtual Information? Info { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
    }
}
