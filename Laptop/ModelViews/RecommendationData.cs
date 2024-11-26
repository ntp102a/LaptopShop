using Microsoft.ML.Data;

namespace LaptopShop.ModelViews
{
    public class RecommendationData
    {
        [LoadColumn(0)]
        public string UserId { get; set; }
        [LoadColumn(1)]
        public string ProductId { get; set; }
    }
}
