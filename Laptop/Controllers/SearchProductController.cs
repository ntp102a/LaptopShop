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

        public IActionResult SearchingProduct(string keyword)
        {
            var lsProduct = _context.Products
                .AsNoTracking()
                .Include(p => p.Image)
                .Include(p => p.Info)
                .Where(x => x.ProductName.Contains(keyword))
                .ToList(); // Lấy danh sách sản phẩm thay vì sử dụng ToPagedList
            var lsCategory = _context.Categories
                    .AsNoTracking()
                    .ToList();
            ViewBag.Categories = lsCategory;
            return View(lsProduct);
        }


    }
}
