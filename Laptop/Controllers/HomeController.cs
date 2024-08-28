using LaptopShop.Models;
using LaptopShop.ModelViews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Newtonsoft.Json;
using System.Diagnostics;

namespace LaptopShop.Controllers
{
    public class HomeController : Controller
    {
       // private readonly ILogger<HomeController> _logger;
        public readonly laptopWebContext _context;

        public HomeController(/*ILogger<HomeController> logger,*/ laptopWebContext context)
        {
            //_logger = logger;
            _context = context;
        }

        // Hàm GetCategories để trả về danh sách danh mục dưới dạng JSON
        public ActionResult GetCategories()
        {
            List<ProductHomeVM> lsProductViews = new List<ProductHomeVM>();
            var lsCats = _context.Categories
                .AsNoTracking()
                .OrderByDescending(x => x.CategoryId)
                .ToList();

            foreach (var item in lsCats)
            {
                ProductHomeVM productHome = new ProductHomeVM();
                productHome.category = item;
                lsProductViews.Add(productHome);
            }


            // Sử dụng JsonConvert để chuyển danh sách danh mục thành chuỗi JSON
            string jsonCategories = JsonConvert.SerializeObject(lsProductViews);

            // Trả về dữ liệu JSON cho yêu cầu
            return Content(jsonCategories, "application/json");
        }

        public IActionResult Index()
        {
            HomeViewVM model = new HomeViewVM();

            var lsProducts = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .OrderByDescending(x => x.ProductId)
                .ToList();

            //Top Selling
            var productCounts = _context.OrderDetails
                .Include(x => x.Product)
                .Include(x => x.Product.Image)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                        {
                            ProductId = g.Key,
                            Count = g.Count()
                        })
                .Where(p => p.Count > 2)
                .Select(p => p.ProductId)
                .ToList();

            ViewBag.TopProducts = productCounts;
            ViewBag.AllProducts = lsProducts;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}