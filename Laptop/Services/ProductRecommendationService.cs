using LaptopShop.Models;
using LaptopShop.ModelViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace LaptopShop.Services
{
    public class ProductRecommendationService
    {
        private MLContext _mlContext;
        private ITransformer _model;
        private readonly laptopWebContext _context;
        public ProductRecommendationService(laptopWebContext context)
        {
            _mlContext = new MLContext();
            _context = context;
        }

        public void TrainModel()
        {
            var productReview = _context.Reviews.ToList();

            var productData = _context.Reviews
                .Select(r => new ProductData
                {
                    UserId = r.UserId.ToString(),
                    ProductId = r.ProductId,
                    Rating = (float)r.Rating
                })
        .ToList();

            // Tải dữ liệu vào IDataView
            var trainData = _mlContext.Data.LoadFromEnumerable(productData);

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("UserId", "UserId")
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("ProductId", "ProductId"))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                labelColumnName: nameof(ProductData.Rating),
                matrixColumnIndexColumnName: "UserId",
                matrixRowIndexColumnName: "ProductId"
            ));

            // Huấn luyện mô hình
            _model = pipeline.Fit(trainData);

            // Lưu mô hình vào file để tái sử dụng
            _mlContext.Model.Save(_model, trainData.Schema, "product_recommendation_model.zip");
        }

        public ProductPrediction GetPrediction(string userId, int productId)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ProductData, ProductPrediction>(_model);

            var prediction = predictionEngine.Predict(new ProductData
            {
                UserId = userId,
                ProductId = productId
            });

            return prediction;
        }

        // Content-Based Filtering: Gợi ý sản phẩm tương tự dựa trên sản phẩm mà người dùng đã mua
        public List<Recommendation> GetContentBasedRecommendations(List<int> boughtProductIds)
        {
            var recommendations = new List<Recommendation>();

            // Lặp qua danh sách các sản phẩm mà người dùng đã mua
            foreach (var productId in boughtProductIds)
            {
                var contentRecommendations = GetSimilarProducts(productId);
                recommendations.AddRange(contentRecommendations);
            }

            // Sắp xếp và loại bỏ các sản phẩm trùng lặp
            return recommendations
                .GroupBy(r => r.ProductId)
                .Select(g => new Recommendation
                {
                    ProductId = g.Key,
                    PredictedRating = g.Average(r => r.PredictedRating) // Tính trung bình điểm đánh giá
                })
                .OrderByDescending(r => r.PredictedRating)
                .ToList();
        }

        // Tìm các sản phẩm tương tự dựa trên thông tin sản phẩm (Content-Based)
        private List<Recommendation> GetSimilarProducts(int productId)
        {
            var targetProduct = _context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (targetProduct == null) return new List<Recommendation>();

            var products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.ProductId != productId)
                .ToList();

            return products.Select(p =>
            {
                var similarity = CalculateCosineSimilarity(targetProduct, p);
                return new Recommendation
                {
                    ProductId = p.ProductId,
                    PredictedRating = (float)similarity // Tương tự như rating (dùng similarity để ước lượng độ yêu thích)
                };
            })
            .OrderByDescending(r => r.PredictedRating)
            .Take(5) // Lấy 5 sản phẩm tương tự nhất
            .ToList();
        }

        // Tính độ tương đồng (Cosine Similarity)
        private double CalculateCosineSimilarity(Product p1, Product p2)
        {
            double brandSimilarity = p1.Category.CategoryName == p2.Category.CategoryName ? 1.0 : 0.0;
            double categorySimilarity = p1.Category == p2.Category ? 1.0 : 0.0;
            //double priceSimilarity = 1.0 - Math.Abs((double)p1.Price - (double)p2.Price) / 1000;
            double priceDifference = Math.Abs((double)p1.Price - (double)p2.Price);
            double priceSimilarity = 1.0 - (priceDifference / Math.Max((double)p1.Price, (double)p2.Price));

            return (brandSimilarity + categorySimilarity + priceSimilarity) / 3;
        }

        // Kết hợp cả Collaborative Filtering và Content-Based Filtering
        public List<Recommendation> GetHybridRecommendations(string userId)
        {
            // Kiểm tra xem người dùng đã có lịch sử mua hàng chưa
            var boughtProductIds = _context.Orders
                .Where(o => o.UserId == userId) // Các đơn hàng của người dùng
                .SelectMany(o => o.OrderDetails.Select(oi => oi.ProductId))
                .ToList();

            List<Recommendation> recommendations;

            if (boughtProductIds.Any()) // Nếu người dùng đã có lịch sử mua hàng
            {
                // Gợi ý sản phẩm dựa trên Content-Based Filtering
                recommendations = GetContentBasedRecommendations(boughtProductIds);
            }
            else // Nếu không có lịch sử mua hàng (tài khoản mới)
            {
                // Gợi ý sản phẩm dựa trên Collaborative Filtering
                recommendations = GetCollaborativeRecommendations(userId);
            }

            return recommendations;
        }

        // Lấy gợi ý từ Collaborative Filtering (Matrix Factorization)
        private List<Recommendation> GetCollaborativeRecommendations(string userId)
        {
            var allProductIds = _context.Products.Select(p => p.ProductId).ToList();

            return allProductIds.Select(productId =>
            {
                var prediction = GetPrediction(userId, productId);
                return new Recommendation
                {
                    ProductId = productId,
                    PredictedRating = Math.Max(prediction.Score, 0)
                };
            })
            .OrderByDescending(p => p.PredictedRating)
            .ToList();
        }
    }
}
