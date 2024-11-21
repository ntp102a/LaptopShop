using LaptopShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopShop.ModelViews;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace LaptopShop.Controllers
{
    [Route("Chatbot")]
    public class ChatbotController : Controller
    {
        private readonly laptopWebContext _context;

        public ChatbotController(laptopWebContext context)
        {
            _context = context;
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

        private async Task<string> GetChatbotResponseAsync(string userInput)
        {
            string lowerInput = userInput.ToLower();

            // Danh sách các từ đồng nghĩa cho các từ khóa
            var keywordSynonyms = new Dictionary<string, List<string>>
    {
        { "lập trình", new List<string> { "lập trình", "dev", "code" } },
        { "đồ họa", new List<string> { "đồ họa", "thiết kế", "vẽ", "3d", "autocad", "photoshop", "video", "hình ảnh" } },
        { "văn phòng", new List<string> { "văn phòng", "office", "soạn thảo", "làm việc", "pin", "di động" } },
        { "game", new List<string> { "game", "điện tử", "valorant", "liên minh", "pubg" } },
        // Thêm các từ đồng nghĩa khác nếu cần
    };

            // Danh sách phản hồi
            var responses = new Dictionary<string, Func<Task<string>>>
    {
        { "lập trình", async () => await GetProductsResponse(p => (p.Description.Contains("lập trình") || p.Info.Cpu.Contains("Core i5") || p.Info.Cpu.Contains("Core i7") || p.Info.Cpu.Contains("Ryzen 5") || p.Info.Cpu.Contains("Ryzen 7")), "Đây là các lựa chọn laptop phù hợp cho học lập trình, hỗ trợ tốt các IDE phổ biến như Visual Studio, IntelliJ, và các công cụ làm việc từ xa:", "lập trình") },
        { "đồ họa", async () => await GetProductsResponse(p => (p.Description.Contains("đồ họa") || p.Info.Cpu.Contains("Core i5")), "Danh sách các laptop dưới đây phù hợp cho việc học thiết kế đồ họa với khả năng xử lý hình ảnh và video hiệu quả, hỗ trợ tốt các phần mềm như Photoshop, Illustrator, và AutoCAD:", "đồ họa") },
        { "văn phòng", async () => await GetProductsResponse(p => p.Description.Contains("văn phòng") || p.Price <= 20000000, "Những laptop dưới đây phù hợp cho công việc văn phòng với thời lượng pin dài, bạn có thể sử dụng cả ngày mà không cần sạc lại, phù hợp cho những người làm việc di động:", "văn phòng") },
        { "game", async () => await GetProductsResponse(p => (p.Info.Vga.Contains("NVIDIA") || p.Info.Vga.Contains("AMD")) && (p.Info.Cpu.Contains("Core i5") || p.Info.Cpu.Contains("Core i7") || p.Info.Cpu.Contains("Ryzen 5")) && (p.Info.Ram.Contains("8GB") || p.Info.Ram.Contains("16GB") || p.Info.Ram.Contains("32GB")) && p.Price >= 15000000, "Dưới đây là danh sách các laptop phù hợp cho nhu cầu chơi game của bạn. Bạn có thể tham khảo một số mẫu laptop mạnh mẽ với cấu hình đáp ứng tốt cho các game phổ biến như FIFA, PUBG, hay Genshin Impact:", "game") }
    };

            // Kiểm tra xem câu đầu vào có chứa từ khóa hay không
            foreach (var entry in keywordSynonyms)
            {
                foreach (var synonym in entry.Value)
                {
                    if (lowerInput.Contains(synonym))
                    {
                        // Xử lý theo từ khóa chính
                        return await responses[entry.Key]();
                    }
                }
            }

            // Xử lý các câu hỏi không rõ
            var unknownResponses = new List<string>
    {
        "Xin lỗi, tôi không hiểu câu hỏi của bạn. Bạn có thể nói rõ hơn không?",
        "Tôi chưa hiểu yêu cầu của bạn. Bạn có thể thử lại với từ khóa khác không? Chẳng hạn như: để học lập trình, chơi game, xử lý đồ họa, tầm giá bao nhiêu?",
        "Xin lỗi, tôi chưa có thông tin về điều này. Bạn muốn hỏi về sản phẩm nào?"
    };

            // Chọn ngẫu nhiên một câu trả lời từ danh sách
            var random = new Random();
            int randomIndex = random.Next(unknownResponses.Count);
            return unknownResponses[randomIndex];
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

                default:
                    return "Không tìm thấy sản phẩm phù hợp";
            }

            var filteredProducts = products.Where(filterCondition).ToList();


            if (filteredProducts.Any())
            {
                var productHtml = filteredProducts.Select(p => GenerateProductHtml(p)).ToList();
                return $"<strong>{message}<br/><br/></strong><div class='product-widget'>" + string.Join("", productHtml) + "</div>";
            }

            return "Xin lỗi, không có sản phẩm nào trong tầm giá này.";
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


    }
}
