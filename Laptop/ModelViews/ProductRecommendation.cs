namespace LaptopShop.ModelViews
{
    public class ProductData
    {
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public float Rating { get; set; }
    }

    public class ProductPrediction
    {
        public float Label { get; set; }
        public float Score { get; set; }
    }
    public class Recommendation
    {
        public int ProductId { get; set; }
        public float PredictedRating { get; set; }
    }
}
