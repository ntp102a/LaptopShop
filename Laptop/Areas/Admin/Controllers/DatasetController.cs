using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaptopShop.Extension;
using System.Text;
using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using LaptopShop.ModelViews;

namespace LaptopShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class DatasetController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }

        public DatasetController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost] // Ensure this is a POST action
        public IActionResult Generate()
        {
            var result = GenerateDataset(); // Gọi phương thức tạo dataset

            if (result) // Nếu thành công
            {
                _notyfService.Success("Dataset generated successfully! You can find it in the 'wwwroot/Files' folder."); // Thông báo thành công
                return Ok(); // Trả về thành công
            }
            else
            {
                return BadRequest("Failed to generate dataset."); // Thông báo lỗi nếu không tạo được file
            }
        }

        public bool GenerateDataset()
        {
            try
            {
                var purchaseHistory = _context.OrderDetails.Include(p => p.Order).Select(p => new RecommendationData
                {
                    UserId = p.Order.UserId,
                    ProductId = p.ProductId.ToString()
                }).ToList();

                // Lấy dữ liệu từ giỏ hàng
                var cartData = _context.Carts
                    .Select(c => new RecommendationData
                    {
                        UserId = c.UserId,
                        ProductId = c.ProductId.ToString()
                    })
                    .ToList();

                // Lấy dữ liệu từ lịch sử xem
                //var viewHistory = _context.ViewHistories
                //    .Select(v => new
                //    {
                //        CustomerId = v.UserId,
                //        ProductId = v.ProductId,
                //        Action = "View",
                //        ActionDate = v.ViewDate,
                //        Quantity = 1
                //    })
                //    .ToList();

                var allData = purchaseHistory
                    .Concat(cartData)
                    .ToList();

                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("UserId,ProductId,Action");

                foreach (var data in allData)
                {
                    var values = new List<string>
                    {
                        data.UserId,
                        data.ProductId.ToString()
                    };

                    csvBuilder.AppendLine(string.Join(",", values.Select(v => $"\"{v}\"")));
                }

                // Đường dẫn tới thư mục Files trong wwwroot
                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath); // Tạo thư mục nếu chưa tồn tại
                }

                var filePath = Path.Combine(directoryPath, "dataset.csv");

                // Ghi nội dung vào file với UTF-8 BOM
                using (var streamWriter = new StreamWriter(filePath, false, new UTF8Encoding(true)))
                {
                    streamWriter.Write(csvBuilder.ToString()); // Ghi nội dung vào file
                }

                return true; // Trả về true nếu thành công
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.Message);
                return false; // Trả về false nếu có lỗi
            }
        }
    }
}
