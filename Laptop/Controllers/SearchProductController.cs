using LaptopShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PagedList.Core;

namespace LaptopShop.Controllers
{
    public class SearchProductController : Controller
    {
        private readonly laptopWebContext _context;

        public SearchProductController(laptopWebContext context)
        {
            _context = context;
        }

        [Route("/search", Name = "SearchingProduct")]
        public IActionResult SearchingProduct(string q)
        {
            var lsProduct = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .Include(p => p.Info)
                .Where(x => x.ProductName.Contains(q))
                .ToList();
            var lsCategory = _context.Categories
                    .AsNoTracking()
                    .ToList();
            ViewBag.Categories = lsCategory;
            ViewBag.ListProduct = lsProduct;

            return View();
        }

        public IActionResult GetProductData(List<int> categoryIds, string sort, List<int> productIds)
        {
            // Truy vấn sản phẩm từ cơ sở dữ liệu dựa trên productIds
            var products = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .Include(p => p.Info)
                .Where(p => productIds.Contains(p.ProductId))
                .Select(p => new
                {
                    Product = p,
                    FinalPrice = (decimal)p.Price * (100 - (decimal)p.Discount) / 100
                });

            // Lọc theo categoryIds
            if (categoryIds != null && categoryIds.Any())
            {
                products = products.Where(p => categoryIds.Contains(p.Product.CategoryId));
            }



            // Sắp xếp nếu có
            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort)
                {
                    case "1":
                        products = products.OrderBy(p => p.FinalPrice);
                        break;
                    case "2":
                        products = products.OrderByDescending(p => p.FinalPrice);
                        break;
                    case "3":
                        products = products.OrderBy(p => p.Product.ProductName);
                        break;
                    case "4":
                        products = products.OrderByDescending(p => p.Product.ProductName);
                        break;
                    default:
                        products = products.OrderByDescending(p => p.Product.Info.Date);
                        break;
                }
            }

            var result = products.Select(p => p.Product).ToList();

            // Trả về partial view
            return PartialView("_SearchProductPartialView", result);
        }


        //public IActionResult GetProductData(List<int> categoryIds, int? sort, List<Product> searchProducts)
        //{
        //    var query = _context.Products.AsQueryable();

        //    if (categoryIds != null && categoryIds.Any())
        //    {
        //        query = query.Where(p => categoryIds.Contains(p.CategoryId));
        //    }

        //    query = query
        //        .AsNoTracking()
        //        .Include(p => p.Image)
        //        .Include(p => p.Info);

        //    switch (sort)
        //    {
        //        case 1:
        //            query = query.OrderBy(x => x.Price);
        //            break;
        //        case 2:
        //            query = query.OrderByDescending(x => x.Price);
        //            break;
        //        default:
        //            query = query.OrderByDescending(x => x.Info.Date);
        //            break;
        //    }

        //    var lsProducts = query.ToList();

        //    return PartialView("_ProductsPartialView", lsProducts);
        //}
    }
}
