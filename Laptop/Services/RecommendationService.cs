//using LaptopShop.ModelViews;
//using Microsoft.ML;
//using Microsoft.ML.Data;
//using Microsoft.ML.Trainers;

//public class RecommendationService
//{
//    private readonly MLContext _mlContext;
//    private ITransformer _model;

//    public RecommendationService()
//    {
//        _mlContext = new MLContext();

//        // Train model ngay khi khởi tạo service
//        TrainModel();
//    }

//    private void TrainModel()
//    {
//        // Load dữ liệu từ file CSV
//        var dataPath = "wwwroot/Files/dataset.csv";
//        var dataView = _mlContext.Data.LoadFromTextFile<RecommendationData>(dataPath, hasHeader: true, separatorChar: ',');

//        // Chuẩn bị pipeline
//        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("UserId")
//            .Append(_mlContext.Transforms.Conversion.MapValueToKey("ProductId"))
//            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
//                new MatrixFactorizationTrainer.Options
//                {
//                    MatrixColumnIndexColumnName = "UserId",
//                    MatrixRowIndexColumnName = "ProductId",
//                    NumberOfIterations = 20,
//                    ApproximationRank = 100
//                }));

//        // Train model
//        _model = pipeline.Fit(dataView);
//    }

//    public List<ProductRecommendation> RecommendForUser(string userId, int topN = 10)
//    {
//        var predictionEngine = _mlContext.Model.CreatePredictionEngine<RecommendationData, ProductRecommendation>(_model);

//        var recommendedProducts = new List<ProductRecommendation>();

//        for (uint productId = 1; productId <= 200; productId++) // Sử dụng uint thay vì int
//        {
//            var prediction = predictionEngine.Predict(new RecommendationData
//            {
//                UserId = userId,
//                ProductId = productId.ToString() // Chuyển sang chuỗi nếu cần
//            });

//            recommendedProducts.Add(new ProductRecommendation
//            {
//                ProductId = productId,
//                Score = prediction.Score
//            });
//        }

//        return recommendedProducts.OrderByDescending(r => r.Score).Take(topN).ToList();
//    }

//}
