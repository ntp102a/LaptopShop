using LaptopShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopShop.ModelViews;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.ML;
using Microsoft.AspNetCore.Components.Forms;
using LaptopShop.Helpper;

namespace LaptopShop.Controllers
{
    [Route("Chatbot")]
    public class ChatbotController : Controller
    {
        private readonly laptopWebContext _context;
        private readonly MLContext _mlContext;
        public ChatbotController(laptopWebContext context)
        {
            _context = context;
            _mlContext = new MLContext();
        }
        public class InputData
        {
            public string Text { get; set; }
        }

        public class OutputData
        {
            public string PredictedCategory { get; set; }
        }

        [HttpPost("GetResponse")]
        public async Task<IActionResult> GetResponse([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserInput))
            {
                return BadRequest(new { response = "Không có đầu vào." });
            }

            // Gọi một phương thức để xử lý thông điệp của người dùng và nhận phản hồi
            string responseMessage = await GetChatbotResponseAsync(request.UserInput);
            return Ok(new { response = responseMessage });
        }
        /*
        public string ProcessText(string inputData)
        {
            try
            {
                var trainData = new[]
                {
                    new InputData { Text = "do hoa" },
                    new InputData { Text = "dohoa" },
                    new InputData { Text = "đồ họa" },
                    new InputData { Text = "thiet ke do hoa" },
                    new InputData { Text = "thiet ke đồ họa" }
                };

                var trainDataView = _mlContext.Data.LoadFromEnumerable(trainData);

                var pipeline = _mlContext.Transforms.Text.FeaturizeText("Text", "Features")
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy());

                Console.WriteLine("Dữ liệu đầu vào có schema như sau:");
                var schema = trainDataView.Schema;
                foreach (var column in schema)
                {
                    Console.WriteLine($"{column.Name}: {column.Type}");
                }

                Console.WriteLine("Bắt đầu huấn luyện mô hình...");
                var model = pipeline.Fit(trainDataView);
                Console.WriteLine("Mô hình huấn luyện hoàn tất.");

                var processedInput = inputData.ToLower();
                processedInput = new string(processedInput.Where(c => !char.IsPunctuation(c)).ToArray());

                var keywords = processedInput.Split(' ').Where(word => word != "").ToList();

                var predictionResults = new List<string>();

                var inputDataForPrediction = new[] { new InputData { Text = string.Join(" ", keywords) } };
                var predictionDataView = _mlContext.Data.LoadFromEnumerable(inputDataForPrediction);
                var predictions = model.Transform(predictionDataView);

                var predictedLabels = _mlContext.Data.CreateEnumerable<OutputData>(predictions, reuseRowObject: false).ToList();
                foreach (var prediction in predictedLabels)
                {
                    predictionResults.Add(prediction.PredictedCategory);
                }

                return string.Join(", ", predictionResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Có lỗi xảy ra: {ex.Message}");
                return string.Empty;
            }
        }
        */
        private async Task<string> GetChatbotResponseAsync(string userInput)
        {
            string lowerInput = userInput.ToLower();

            var matchedKeywords = new HashSet<string>();

            // Danh sách các từ đồng nghĩa cho các từ khóa
            var keywordSynonyms = new Dictionary<string, List<string>>
            {

                { "lập trình", new List<string> { "lập trình", "dev", "code", "học lập trình", "laptop cho lập trình", "máy tính lập trình", "laptop cho dev", "laptop code", "lap trinh", "laptrinh", "devloper", "devlop", "laptop lap trinh", "lap trình" } },
                { "đồ họa", new List<string> { "đồ họa", "thiết kế", "vẽ", "3d", "autocad", "photoshop", "video", "hình ảnh", "xử lý ảnh", "laptop cho đồ họa", "máy tính thiết kế", "đo hoa", "đohoa", "đò hoa", "xư ly anh", "phôtoshop" } },
                { "văn phòng", new List<string> { "văn phòng", "office", "soạn thảo", "làm việc", "pin", "di động", "laptop công việc", "máy tính văn phòng", "laptop cho văn phòng", "van phong", "vănphong, van phòng", "cong viẹc", "word", "excel" } },
                { "game", new List<string> { "game", "gaming", "chơi game", "game thủ", "laptop chơi game", "máy tính chơi game", "laptop gaming", "game điện tử", "game pc", "máy tính game", "choi diện tư" } },
                { "nhỏ gọn", new List<string> { "nhỏ gọn", "di động", "mỏng nhẹ", "laptop nhẹ", "máy tính di động", "máy tính nhẹ", "laptop 14 inch", "laptop 13 inch", "nhỏgon", "nho gọn" } },
                { "cấu hình mạnh", new List<string> { "cấu hình mạnh", "mạnh mẽ", "tốc độ cao", "cấu hình cao", "laptop cấu hình mạnh", "máy tính chơi game", "laptop i7", "laptop core i7" } },
                { "bảo hành", new List<string> { "bảo hành", "thời gian bảo hành", "chính sách bảo hành", "bao hành", "bảohanh", } },
                { "giao hàng", new List<string> { "giao hàng", "thời gian giao hàng", "vận chuyển" } },
                { "hỗ trợ kỹ thuật", new List<string> { "hỗ trợ kỹ thuật", "hỗ trợ sản phẩm", "dịch vụ sau bán hàng" } },
                { "chào", new List<string> { "chào", "xin chào", "hello", "hi", "chào bạn", "chào anh", "chào chị" } },
                { "mua laptop", new List<string> { "mua laptop", "tôi muốn mua laptop", "tìm laptop", "mua máy tính", "tìm máy tính" } },
                { "tư vấn", new List<string> { "tư vấn", "giúp tôi", "tư vấn laptop", "cần tư vấn", "giúp tôi chọn laptop", "có thể tư vấn cho tôi" } },
                { "trả góp", new List<string> { "trả góp", "mua trả góp", "trả trước", "mua không trả tiền trước", "mua góp" } },
                { "chi tiết sản phẩm", new List<string> { "chi tiết", "thông tin chi tiết", "cấu hình chi tiết", "thông số sản phẩm", "tìm hiểu sản phẩm", "xem chi tiết", "thông tin" } },
                { "dưới 10 triệu", new List<string> { "dưới 10 triệu", "khoảng 10 triệu", "tầm 10 triệu", "dưới 10tr", "khoảng 10tr", "tầm 10tr" } },
                { "trên 10 triệu", new List<string> { "trên 10 triệu", "khoảng trên 10 triệu", "trên 10tr", "khoảng trên 10tr" } },
                { "dưới 20 triệu", new List<string> { "dưới 20 triệu", "khoảng 20 triệu", "tầm 20 triệu", "dưới 20tr", "khoảng 20tr", "tầm 20tr" } },
                { "trên 20 triệu", new List<string> { "trên 20 triệu", "khoảng trên 20 triệu", "trên 20tr", "khoảng trên 20tr" } },
                { "dưới 30 triệu", new List<string> { "dưới 30 triệu", "khoảng 30 triệu", "tầm 30 triệu", "dưới 30tr", "khoảng 30tr", "tầm 30tr" } },
                { "trên 30 triệu", new List<string> { "trên 30 triệu", "khoảng 30tr" } },
                { "dưới 40 triệu", new List<string> { "dưới 40 triệu", "khoảng 40 triệu", "tầm 40 triệu", "dưới 40tr", "khoảng 40tr", "tầm 40tr" } }
            };

            // Danh sách phản hồi
            var responses = new Dictionary<string, Func<Task<string>>>
            {
                { "lập trình", async () => await GetProductsResponse(p => (p.Description.Contains("lập trình") || p.Info.Cpu.Contains("Core i5") || p.Info.Cpu.Contains("Core i7") || p.Info.Cpu.Contains("Ryzen 5") || p.Info.Cpu.Contains("Ryzen 7") && (p.Info.Ram.Contains("16GB") || p.Info.Ram.Contains("32GB"))), "Đây là các lựa chọn laptop phù hợp cho học lập trình, hỗ trợ tốt các IDE phổ biến như Visual Studio, IntelliJ, và các công cụ làm việc từ xa:", "lập trình") },

                { "đồ họa", async () => await GetProductsResponse(p => (p.Description.Contains("đồ họa") || p.Info.Cpu.Contains("Core i5")), "Danh sách các laptop dưới đây phù hợp cho việc học thiết kế đồ họa với khả năng xử lý hình ảnh và video hiệu quả, hỗ trợ tốt các phần mềm như Photoshop, Illustrator, và AutoCAD:", "đồ họa") },

                { "văn phòng", async () => await GetProductsResponse(p => p.Description.Contains("văn phòng") || p.Price <= 20000000, "Những laptop dưới đây phù hợp cho công việc văn phòng với thời lượng pin dài, bạn có thể sử dụng cả ngày mà không cần sạc lại, phù hợp cho những người làm việc di động:", "văn phòng") },

                { "game", async () => await GetProductsResponse(p => (p.Info.Vga.Contains("NVIDIA") || p.Info.Vga.Contains("AMD")) && (p.Info.Cpu.Contains("Core i5") || p.Info.Cpu.Contains("Core i7") || p.Info.Cpu.Contains("Ryzen 5")) && (p.Info.Ram.Contains("8GB") || p.Info.Ram.Contains("16GB") || p.Info.Ram.Contains("32GB")) && p.Price >= 15000000, "Dưới đây là danh sách các laptop phù hợp cho nhu cầu chơi game của bạn. Bạn có thể tham khảo một số mẫu laptop mạnh mẽ với cấu hình đáp ứng tốt cho các game phổ biến như FIFA, PUBG, hay Genshin Impact:", "game") },

                { "nhỏ gọn", async () => await GetProductsResponse(p => ExtractScreenSize(p.Info.Screen) <= 14 && ExtractWeightInKg(p.Info.Design) <= 2.0, "Dưới đây là các laptop nhỏ gọn và di động, dễ dàng mang theo khi di chuyển:", "nhỏ gọn") },

                { "cấu hình mạnh", async () => await GetProductsResponse(p => (p.Info.Cpu.Contains("Core i7") || p.Info.Cpu.Contains("Ryzen 7") && (p.Info.Ram.Contains("16GB") || p.Info.Ram.Contains("32GB"))), "Dưới đây là các laptop có cấu hình mạnh mẽ với CPU mạnh, phù hợp cho những công việc yêu cầu hiệu suất cao như render video, chơi game, hoặc các ứng dụng đòi hỏi tài nguyên lớn:", "cấu hình mạnh") },

                { "bảo hành", async () => "Tất cả sản phẩm của chúng tôi đều bảo hành đổi trả 1-1 trong vòng 15 ngày đầu nếu phát sinh ra lỗi. Sau 15 ngày thì sẽ được đem lên hãng bảo hành. Chi tiết xin liên hệ nhân viên của chúng tôi." },

                { "khuyến mãi", async () => "Bạn muốn biết về các chương trình khuyến mãi hiện tại?" },

                { "giao hàng", async () => "Thời gian giao hàng có thể khác nhau tùy theo địa điểm của bạn. Trung bình sẽ từ 3-7 ngày bạn nhé. Chúng tôi sẽ liên hệ với bạn ngay khi nhân viên đi giao máy." },

                { "hỗ trợ kỹ thuật", async () => "Bạn cần hỗ trợ kỹ thuật cho sản phẩm nào?" },

                { "chào", async () => "Chào bạn! Tôi có thể giúp gì cho bạn hôm nay?" },

                { "mua laptop", async () =>"Bạn đang tìm mua laptop? Có thể bạn cần chọn lựa theo mục đích sử dụng như lập trình, đồ họa, chơi game, hay công việc văn phòng?" },

                { "tư vấn", async () => "Bạn cần tư vấn về laptop nào? Mục đích sử dụng và ngân sách của bạn là gì?"},

                { "trả góp", async () => "Chúng tôi hỗ trợ trả góp với lãi suất 0% qua các ngân hàng liên kết. Bạn có thể thanh toán bằng thẻ tín dụng hoặc trả góp qua các đơn vị tài chính uy tín. Vui lòng liên hệ với nhân viên của chúng tôi để được tư vấn đầy đủ hơn. 0336960995 (Phước)."},
                { "dưới 10 triệu", async () => await GetProductsResponse(p => p.Price <= 10000000, "Danh sách các laptop dưới 10 triệu:", "dưới 10 triệu") },
                { "trên 10 triệu", async () => await GetProductsResponse(p => p.Price > 10000000, "Danh sách các laptop trên 10 triệu:", "trên 10 triệu") },
                { "dưới 20 triệu", async () => await GetProductsResponse(p => p.Price <= 20000000, "Danh sách các laptop dưới 20 triệu:", "dưới 20 triệu") },
                { "trên 20 triệu", async () => await GetProductsResponse(p => p.Price > 20000000, "Danh sách các laptop trên 20 triệu:", "trên 20 triệu") },
                { "dưới 30 triệu", async () => await GetProductsResponse(p => p.Price <= 30000000, "Danh sách các laptop dưới 30 triệu:", "dưới 30 triệu") },
                { "trên 30 triệu", async () => await GetProductsResponse(p => p.Price > 30000000, "Danh sách các laptop trên 30 triệu:", "trên 30 triệu") },
                { "dưới 40 triệu", async () => await GetProductsResponse(p => p.Price <= 40000000, "Danh sách các laptop dưới 40 triệu:", "dưới 40 triệu") },
                { "trên 40 triệu", async () => await GetProductsResponse(p => p.Price > 40000000, "Danh sách các laptop trên 40 triệu:", "trên 40 triệu") },
            };

            if (lowerInput.Contains("có gì nổi bật") || lowerInput.Contains("nổi bật") || lowerInput.Contains("cấu hình") || lowerInput.Contains("đặc điểm") || lowerInput.Contains("tính năng") || lowerInput.Contains("thông số") || lowerInput.Contains("chi tiết") || lowerInput.Contains("giới thiệu") || lowerInput.Contains("mô tả") || lowerInput.Contains("điểm mạnh") || lowerInput.Contains("ưu điểm") || lowerInput.Contains("lý do chọn") || lowerInput.Contains("có gì hay") || lowerInput.Contains("đánh giá"))
            {
                // Tách thông tin tên hoặc mã sản phẩm
                string productNameOrId = ExtractProductName(lowerInput);

                // Kiểm tra nếu không có thông tin tên sản phẩm
                if (string.IsNullOrEmpty(productNameOrId))
                {
                    return "Bạn chưa cung cấp tên hoặc mã sản phẩm. Vui lòng thử lại với cú pháp: *Sản phẩm <Tên sản phẩm> có gì nổi bật?*";
                }

                // Tìm sản phẩm trong cơ sở dữ liệu
                var productDetails = await _context.Products
                    .Include(p => p.Info)
                    .Include(p => p.Image)
                    .Where(p => (p.ProductName.Contains(productNameOrId) || p.ProductId.ToString() == productNameOrId) && (p.IsPublic == true))
                    .Select(p => new
                    {
                        p.ProductName,
                        p.Price,
                        p.Description,
                        p.Info.Cpu,
                        p.Info.Ram,
                        p.Info.Hardware,
                        p.Info.Screen,
                        p.Info.Design,
                        p.Image.ImageThumb
                    })
                    .FirstOrDefaultAsync();

                // Nếu không tìm thấy sản phẩm
                if (productDetails == null)
                {
                    return $"Xin lỗi, tôi không tìm thấy sản phẩm với thông tin: *{productNameOrId}*. Bạn có thể kiểm tra lại tên hoặc mã sản phẩm.";
                }

                // Trả về thông tin nổi bật của sản phẩm
                return $@"<strong>Điểm nổi bật của {productDetails.ProductName}:</strong><br/><img src='images/products/{productDetails.ImageThumb}' alt='{productDetails.ProductName}'/><br/><strong>Giá:</strong> {productDetails.Price:N0} VND<br/><strong>Cấu hình:</strong><br/>- CPU: {productDetails.Cpu}<br/>- RAM: {productDetails.Ram}<br/>- Lưu trữ: {productDetails.Hardware}<br/>- Màn hình: {productDetails.Screen}<br/>- Thiết kế: {productDetails.Design}<br/>";
            }

            var extendedKeywordSynonyms = new Dictionary<string, List<string>>();
            foreach (var keyword in keywordSynonyms)
            {
                var expandedSynonyms = new List<string>();
                foreach (var synonym in keyword.Value)
                {
                    // Thêm từ không dấu
                    expandedSynonyms.Add(KeywordHelper.RemoveDiacritics(synonym));

                    // Thêm biến thể sai chính tả
                    expandedSynonyms.AddRange(KeywordHelper.GenerateMisspelledKeywords(synonym));
                }

                // Gộp danh sách gốc với danh sách mở rộng
                expandedSynonyms.AddRange(keyword.Value);
                extendedKeywordSynonyms[keyword.Key] = expandedSynonyms.Distinct().ToList();
            }


            // Kiểm tra xem câu đầu vào có chứa từ khóa hay không
            foreach (var entry in extendedKeywordSynonyms)
            {
                foreach (var synonym in entry.Value)
                {
                    if (lowerInput.Contains(synonym))
                    {
                        matchedKeywords.Add(entry.Key);
                        break;
                    }
                }
            }

            if (!matchedKeywords.Any())
            {
                var unknownResponses = new List<string>
                {
                    "Xin lỗi, tôi không hiểu câu hỏi của bạn. Bạn có thể nói rõ hơn không?",
                    "Tôi chưa hiểu yêu cầu của bạn. Bạn có thể thử lại với từ khóa khác không? Chẳng hạn như: để học lập trình, chơi game, xử lý đồ họa, tầm giá bao nhiêu?",
                    "Xin lỗi, tôi chưa có thông tin về điều này. Bạn muốn hỏi về sản phẩm nào?"
                };
                var random = new Random();
                return unknownResponses[random.Next(unknownResponses.Count)];
            }
            // Xử lý nhiều từ khóa khớp
            var responsesForKeywords = new List<string>();
            foreach (var keyword in matchedKeywords)
            {
                if (responses.ContainsKey(keyword))
                {
                    var response = await responses[keyword]();
                    responsesForKeywords.Add(response);
                }
            }

            // Ghép các phản hồi lại
            return string.Join("<br/><br/>", responsesForKeywords);
        }

        private async Task<string> GetProductsResponse(Expression<Func<Product, bool>> filter, string message, string key)
        {
            var products = await _context.Products
                .Include(p => p.Info)
                .Where(filter)
                .Select(p => new { p.ProductId, p.ProductName, p.Price, p.Image.ImageThumb, p.Info.Design, p.Info.Cpu, p.Info.Screen })
                .ToListAsync();

            Func<dynamic, bool> filterCondition = null;

            switch (key)
            {
                case "văn phòng":
                    filterCondition = p => !string.IsNullOrEmpty(p.Design) && IsPowerEfficientCpu(p.Cpu);
                    break;
                case "lập trình":
                    filterCondition = p => !string.IsNullOrEmpty(p.Design) && ExtractBatteryCapacity(p.Design) >= 50;
                    break;
                case "đồ họa":
                    filterCondition = p => !string.IsNullOrEmpty(p.Design) &&
                                   ExtractScreenSize(p.Screen) >= 15;
                    break;
                case "game":
                    filterCondition = p => !string.IsNullOrEmpty(p.Design) &&
                                  ExtractScreenSize(p.Screen) >= 15;
                    break;

                case "nhỏ gọn":
                    filterCondition = p => !string.IsNullOrEmpty(p.Design) &&
                                           ExtractBatteryCapacity(p.Design) >= 50 &&
                                           ExtractScreenSize(p.Screen) <= 15 &&
                                           ExtractWeightInKg(p.Design) <= 2;
                    break;
                case "cấu hình mạnh":
                    filterCondition = p => !string.IsNullOrEmpty(p.Design) && ExtractScreenSize(p.Screen) >= 14;
                    break;
                case "dưới 10 triệu":
                    var productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                case "trên 10 triệu":
                    productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                case "dưới 20 triệu":
                    productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                case "trên 20 triệu":
                    productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                case "dưới 30 triệu":
                    productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                case "trên 30 triệu":
                    productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                case "dưới 40 triệu":
                    productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                case "trên 40 triệu":
                    productHtml = products.Select(p => GenerateProductHtml(p)).ToList();
                    return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
                default:
                    return "Không tìm thấy sản phẩm phù hợp";
            }

            var filteredProducts = products.Where(filterCondition).ToList();


            if (filteredProducts.Any())
            {
                var productHtml = filteredProducts.Select(p => GenerateProductHtml(p)).ToList();
                return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
            }

            return "Xin lỗi, hiện tại chúng tôi chưa có sản phẩm như yêu cầu.";
        }

        private string GenerateProductHtml(dynamic product)
        {
            string url = $"/san-pham/{product.ProductName}";

            return
                $"<div class='product-widget'>" +
                $"<div class='product-img'>" +
                $"<img src='/images/products/{product.ImageThumb}' alt='{product.ProductName}' />" +
                $"</div>" +
                $"<div class='product-body'>" +
                $"<h3 class='product-name'><a href='{url}' target='_blank'>{product.ProductName}</a></h3>" +
                $"<h4 class='product-price'>{product.Price:N0} VNĐ</h4>" +
                $"</div>" +
                $"</div>";
        }

        private int ExtractBatteryCapacity(string design)
        {
            if (string.IsNullOrEmpty(design))
                return 0;

            // Tìm kiếm giá trị pin chứa "Whr" và lấy giá trị số
            var match = Regex.Match(design, @"(\d+)\s*Whr", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int capacity))
            {
                return capacity;
            }

            return 0;
        }

        //kiếm dòng U-series
        private bool IsPowerEfficientCpu(string cpu)
        {
            // Biểu thức chính quy để nhận diện CPU U-series
            var regex = new Regex(@"\b([A-Za-z]+\s*Core\s*i[0-9]+|[A-Za-z]+\s*Ryzen\s*[0-9]+)\s*-?\s*\d+U\b", RegexOptions.IgnoreCase);

            // Kiểm tra nếu chuỗi mô tả CPU khớp với biểu thức chính quy
            return regex.IsMatch(cpu);
        }

        private double? ExtractScreenSize(string design)
        {
            if (string.IsNullOrEmpty(design))
                return null;

            // Regex để tìm kích thước màn hình trong định dạng "X.X inch" hoặc "X inch"
            var regex = new Regex(@"(\d+(\.\d{1,2})?)\s*""", RegexOptions.IgnoreCase);
            var match = regex.Match(design);

            if (match.Success && double.TryParse(match.Groups[1].Value, out double screenSize))
            {
                return screenSize;
            }

            return null; // Trả về null nếu không tìm thấy
        }

        private double? ExtractWeightInKg(string design)
        {
            if (string.IsNullOrEmpty(design))
                return null;

            // Regex để tìm trọng lượng trong định dạng "X kg" hoặc "X.x kg"
            var regex = new Regex(@"(\d+(\.\d{1,2})?)\s*kg", RegexOptions.IgnoreCase);
            var match = regex.Match(design);

            if (match.Success && double.TryParse(match.Groups[1].Value, out double weight))
            {
                return weight;
            }

            return null;
        }
        private string ExtractProductName(string input)
        {
            // Chuyển văn bản thành chữ thường và loại bỏ ký tự đặc biệt
            string cleanedInput = input.ToLower().Replace("có gì nổi bật", "")
                                                .Replace("sản phẩm", "")
                                                .Replace("?", "")
                                                .Trim();

            // Regex để tìm tên sản phẩm
            var regex = new Regex(@"\b(?:lenovo|dell|hp|asus|acer|msi|apple|razer|microsoft)\s[\w\s]+(?:\d{2}[a-z0-9]*)\b", RegexOptions.IgnoreCase);
            var match = regex.Match(cleanedInput);

            return match.Success ? match.Value.Trim() : cleanedInput;
        }


    }
}
