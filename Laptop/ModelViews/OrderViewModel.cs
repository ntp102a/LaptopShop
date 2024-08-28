namespace LaptopShop.ModelViews
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string? RecipientName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? Total { get; set; }
        public string? Note { get; set; }
        public int? UserId { get; set; }
        public int? StatusId { get; set; }
    }
}
