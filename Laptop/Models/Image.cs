using System;
using System.Collections.Generic;

namespace LaptopShop.Models
{
    public partial class Image
    {
        public Image()
        {
            Products = new HashSet<Product>();
        }

        public int ImageId { get; set; }
        public string? ImageThumb { get; set; }
        public string? Image1 { get; set; }
        public string? Image2 { get; set; }
        public string? Image3 { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
