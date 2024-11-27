namespace LaptopShop.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; }
        public int Rating { get; set; } // 1-5 sao
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Product Products { get; set; }
        public virtual User Users { get; set; }
    }
}
