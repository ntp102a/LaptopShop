using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Models;
using LaptopShop.ModelViews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PagedList.Core;
using System.Linq;

namespace LaptopShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }
        public ProductController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        [Route("/tat-ca-san-pham", Name = "ShopProduct")]
        public IActionResult Index()
        {
            try
            {
                var lsProducts = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .Include(p => p.Info)
                .Where(p => p.IsPublic == true)
                .OrderByDescending(x => x.Info.Date)
                .ToList();

                ViewBag.lsProduct = lsProducts;

                var lsCategory = _context.Categories
                    .AsNoTracking()
                    .ToList();
                ViewBag.Categories = lsCategory;
                return View();
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }

        //public IActionResult Index(int? page, int? pagesize, int? sort)
        //{
        //    try
        //    {
        //        //var pageNumber = page == null || page <= 0 ? 1 : page.Value;
        //        //var pageSize = pagesize ?? 12;

        //        var lsProducts = _context.Products
        //        .AsNoTracking()
        //        .Include(p => p.Image)
        //        .Include(p => p.Info)
        //        .OrderByDescending(x => x.Info.Date)
        //        .ToList();


        //        ViewBag.lsProduct = lsProducts;

        //        //IQueryable<Product> lsProduct;
                
        //        //if (sort == 0)
        //        //{
        //        //    lsProduct = _context.Products
        //        //    .AsNoTracking()
        //        //    .Include(p => p.Image)
        //        //    .Include(p => p.Info)
        //        //    .OrderByDescending(x => x.ProductId);
        //        //}
        //        //else if (sort == 1)
        //        //{
        //        //    lsProduct = _context.Products
        //        //    .AsNoTracking()
        //        //    .Include(p => p.Image)
        //        //    .Include(p => p.Info)
        //        //    .OrderBy(x => x.Price);
        //        //}
        //        //else
        //        //{
        //        //    lsProduct = _context.Products
        //        //    .AsNoTracking()
        //        //    .Include(p => p.Image)
        //        //    .Include(p => p.Info)
        //        //    .OrderByDescending(x => x.Price);
        //        //}
        //        var lsCategory = _context.Categories
        //            .AsNoTracking()
        //            .ToList();
        //        //PagedList<Product> models = new PagedList<Product>(lsProducts, pageNumber, pageSize);
        //        //ViewBag.CurrentPages = pageNumber;
        //        ViewBag.Categories = lsCategory;
        //        return View();
        //    }
        //    catch
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //}

        //[Route("/danh-muc/{name}")]
        //public IActionResult List(string name, int page = 1, int pagesize = 0, int sort = 0)
        //{
        //    try
        //    {
        //        var pageSize = 12;
        //        if (pagesize != 0)
        //        {
        //            pageSize = pagesize;
        //        }
        //        var danhmuc = _context.Categories
        //            .AsNoTracking()
        //            .SingleOrDefault(x => x.CategoryName == name);
        //        IQueryable<Product> lsPages;
        //        if (sort == 0)
        //        {
        //            lsPages = _context.Products
        //            .Include(p => p.Image)
        //            .Include(p => p.Category)
        //            .Include(p => p.Info)
        //            .Where(p => p.Category.CategoryName == name)
        //            .AsNoTracking()
        //            .OrderByDescending(x => x.Info.Date);
        //        }
        //        else if (sort == 1)
        //        {
        //            lsPages = _context.Products
        //            .Include(p => p.Image)
        //            .Include(p => p.Category)
        //            .Include(p => p.Info)
        //            .Where(p => p.ProductName == name)
        //            .AsNoTracking()
        //            .OrderBy(x => x.Price);
        //        }
        //        else
        //        {
        //            lsPages = _context.Products
        //            .Include(p => p.Image)
        //            .Include(p => p.Category)
        //            .Include(p => p.Info)
        //            .Where(p => p.Category.CategoryName == name)
        //            .AsNoTracking()
        //            .OrderByDescending(x => x.Price);
        //        }
        //        var lsCategory = _context.Categories
        //            .AsNoTracking()
        //            .ToList();

        //        PagedList<Product> models = new PagedList<Product>(lsPages, page, pageSize);
        //        ViewBag.CurrentPage = page;
        //        ViewBag.CurrentCat = danhmuc;
        //        ViewBag.Categories = lsCategory;
        //        return View(models);
        //    }
        //    catch
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //}

        [Route("/san-pham/{name}", Name ="Products")]
        public IActionResult Details(string name)
        {
            try
            {
                var product = _context.Products
                    .Include(p => p.Image)
                    .Include(p => p.Info)
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .FirstOrDefault(x => x.ProductName == name);

                if (product == null)
                {
                    return RedirectToAction("Index");
                }

                var reviews = _context.Reviews
                    .Include(p => p.Users)
                    .Include(p => p.Products)
                    .Where(x => x.ProductId == product.ProductId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
                int totalComment = reviews.Count;
                // Tính toán đánh giá trung bình
                var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                // Phân loại Rating Breakdown (Số lượng reviews với từng rating từ 1 đến 5 sao)
                var ratingBreakdown = new List<RatingBreakdownViewModel>();
                for (int i = 1; i <= 5; i++)
                {
                    var count = reviews.Count(r => r.Rating == i);
                    var percentage = reviews.Any() ? (count / (double)reviews.Count) * 100 : 0;
                    ratingBreakdown.Add(new RatingBreakdownViewModel
                    {
                        Star = i,
                        Count = count,
                        Percentage = percentage
                    });
                }
                ViewBag.Reviews = reviews;
                ViewBag.TotalComment = totalComment;
                ViewBag.AverageRating = averageRating;
                ViewBag.RatingBreakdown = ratingBreakdown;

                var lsProduct = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Image)
                    .Include(p => p.Info)
                    .Include(p => p.Category)
                    .Where(x => x.CategoryId == product.CategoryId && x.ProductName != name && x.IsPublic == true)
                    .OrderByDescending(x => x.Price)
                    .Take(4)
                    .ToList();
                ViewBag.Sanpham = lsProduct;
                return View(product);
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }

        }

        [HttpPost]
        public IActionResult CreateComment(Review model)
        {
            var taikhoanID = HttpContext.Session.GetString("UserId");
            var products = _context.Products
                .Include(p => p.Image)
                .Include(p => p.Info)
                .Include(p => p.Category)
                .FirstOrDefault(n => n.ProductId == model.ProductId);
            if (taikhoanID != null)
            {
                var review = new Review
                {
                    ProductId = model.ProductId,
                    UserId = taikhoanID,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now,
                };
                _context.Add(review);
                _context.SaveChanges();

                _notyfService.Success("Thành công");
            }
            else
            {
                HttpContext.Session.SetString("returnUrl", Url.Action("Details", new { name = products.ProductName }));
                _notyfService.Error("Vui lòng đăng nhập để sử dụng tính năng này!");
                return RedirectToAction("Login", "Accounts");
            }
            var reviews = _context.Reviews
                .Include(p => p.Products)
                .Where(p => p.UserId == taikhoanID)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            return RedirectToAction("Details", new { name = reviews.Products.ProductName });
        }
        public IActionResult GetProductData(List<int> categoryIds, List<string> needs, int? sort, int? maxPrice)
        {
            // Truy vấn sản phẩm từ cơ sở dữ liệu dựa trên productIds
            var products = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .Include(p => p.Info)
                .Where(p => p.IsPublic == true)
                .Select(p => new
                {
                    Product = p,
                    FinalPrice = (decimal)p.Price * (100 - (decimal)p.Discount) / 100
                });

            if(maxPrice != null)
            {
                products = products.Where(p => p.FinalPrice <= maxPrice);
            }

            // Lọc theo categoryIds
            if (categoryIds != null && categoryIds.Any())
            {
                products = products.Where(p => categoryIds.Contains(p.Product.CategoryId));
            }

            // Lọc theo nhu cầu sử dụng
            if (needs != null && needs.Count > 0)
            {
                foreach (var need in needs)
                {
                    switch (need)
                    {
                        case "Văn phòng":
                            products = products.Where(p =>
                                (p.Product.Info.Ram.Contains("8GB") || p.Product.Info.Ram.Contains("16GB")) && p.Product.Price <= 20000000);
                            break;

                        case "Đồ họa":
                            products = products.Where(p =>
                                (p.Product.Info.Vga != null &&
                                 (p.Product.Info.Vga.Contains("NVIDIA") || p.Product.Info.Vga.Contains("AMD Radeon"))) && (p.Product.Info.Ram.Contains("16GB") || p.Product.Info.Ram.Contains("32GB")));
                            break;

                        case "Lập trình":
                            products = products.Where(p => (p.Product.Info.Cpu.Contains("i5") || p.Product.Info.Cpu.Contains("i7")) && (p.Product.Info.Ram.Contains("8GB") || p.Product.Info.Ram.Contains("16GB")));
                            break;

                        case "Gaming":
                            products = products.Where(p =>
                                (p.Product.Info.Vga != null && p.Product.Info.Vga.Contains("RTX")) && (p.Product.Info.Ram.Contains("16GB") || p.Product.Info.Ram.Contains("32GB")) && p.Product.Price >= 20000000);
                            break;

                        default:
                            break;
                    }
                }
            }

            // Sắp xếp nếu có
            switch (sort)
            {
                case 1:
                    products = products.OrderBy(p => p.FinalPrice);
                    break;
                case 2:
                    products = products.OrderByDescending(p => p.FinalPrice);
                    break;
                case 3:
                    products = products.OrderBy(p => p.Product.ProductName);
                    break;
                case 4:
                    products = products.OrderByDescending(p => p.Product.ProductName);
                    break;
                default:
                    products = products.OrderByDescending(p => p.Product.Info.Date);
                    break;
            }

            var result = products.Select(p => p.Product).ToList();

            // Trả về partial view
            return PartialView("_ProductsPartialView", result);
        }

    }
}
