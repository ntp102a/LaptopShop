using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PagedList.Core;

namespace LaptopShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class ChangePriceController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }

        public ChangePriceController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }
        public IActionResult Index(int? page)
        {
            var pageNumber = page == null || page <= 0 ? 1 : page.Value;
            var pageSize = 10;
            var lsbrands = _context.Categories
                .AsNoTracking()
                .OrderBy(x => x.CategoryId);
            PagedList<Category> models = new PagedList<Category>(lsbrands, pageNumber, pageSize);
            ViewBag.CurrentPage = pageNumber;
            return View(models);
        }
        public async Task<IActionResult> ChangePriceProducts(int categoryId)
        {
            if (categoryId <= 0 || _context.Orders == null)
            {
                return NotFound();
            }

            var products = await _context.Products
                .Include(x => x.Category)
                .Where(x => x.CategoryId == categoryId)
                .ToListAsync();

            if (products == null || products.Count == 0)
            {
                return NotFound();
            }

            ViewBag.Products = products;
            return PartialView("ChangePriceProducts", products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePriceProducts(int? id, Product product)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var products = await _context.Products.Include(x => x.Category).Where(x => x.CategoryId == id).ToListAsync();
            ViewBag.Products = products;
            if (products == null)
            {
                return NotFound();
            }
            return PartialView("ChangePriceProducts", products);
        }
    }
}
