using System;
using System.Collections.Generic;

namespace LaptopShop.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderDetails = new HashSet<OrderDetail>();
        }

        public int OrderId { get; set; }
        public string? RecipientName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? Total { get; set; }
        public string? Note { get; set; }
        public string UserId { get; set; }
        public int? StatusId { get; set; }
        public bool? IsPayment { get; set; }

        public virtual TransactStatus? Status { get; set; }
        public virtual User? User { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
