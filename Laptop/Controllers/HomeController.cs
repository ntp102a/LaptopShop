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
        public readonly laptopWebContext _context;
        //private readonly RecommendationService _recommendationService;

        public HomeController( laptopWebContext context)
        {
            _context = context;
            //_recommendationService = new RecommendationService();
        }

        public IActionResult Index()
        {
            var lsProducts = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .Where(p => p.IsPublic == true)
                .OrderByDescending(x => x.ProductId)
                .Take(12)
                .ToList();

            //Top Selling
            var productCounts = _context.OrderDetails
                .Include(x => x.Product)
                .Include(x => x.Product.Image)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Count = g.Count(),
                })
                .Where(p => p.Count > 2)
                .Select(p => p.ProductId)
                .Take(12)
                .ToList();

            //Laptop Game
            var gameProducts = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .Where(p => p.Description.Contains("game") && p.IsPublic == true)
                .Take(12)
                .ToList();

            //var taikhoanID = HttpContext.Session.GetString("UserId");
            //if (taikhoanID != null)
            //{
            //    var recommendations = _recommendationService.RecommendForUser(taikhoanID);

            //    List<Product> recomProducts = new List<Product>();

            //    foreach (var recommendation in recommendations)
            //    {
            //        var product = _context.Products
            //            .Include(p => p.Image)
            //            .Include(p => p.Category)
            //            .FirstOrDefault(p => p.ProductId == recommendation.ProductId);
            //        if (product != null)
            //        {
            //            var lsProduct = _context.Products.AsNoTracking()
            //                .Include(p => p.Image)
            //                .Include(p => p.Category)
            //                .Where(x => x.CategoryId == product.CategoryId && x.ProductName != product.ProductName)
            //                .OrderByDescending(x => x.Price)
            //                .ToList();
            //            foreach (var pro in lsProduct)
            //            {
            //                recomProducts.Add(pro);
            //            }
            //        }
            //    }

            //    ViewBag.RecommendProducts = recomProducts;

            //}

            ViewBag.TopProducts = productCounts;
            ViewBag.AllProducts = lsProducts;
            ViewBag.GameProducts = gameProducts;

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