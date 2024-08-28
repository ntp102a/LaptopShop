using System;
using System.Collections.Generic;

namespace LaptopShop.Models
{
    public partial class Information
    {
        public Information()
        {
            Products = new HashSet<Product>();
        }

        public int InfoId { get; set; }
        public string? Cpu { get; set; }
        public string? Ram { get; set; }
        public string? Hardware { get; set; }
        public string? Screen { get; set; }
        public string? Vga { get; set; }
        public string? ConnectGate { get; set; }
        public string? Os { get; set; }
        public string? Design { get; set; }
        public string? Size { get; set; }
        public DateTime? Date { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
