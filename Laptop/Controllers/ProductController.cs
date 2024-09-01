using LaptopShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PagedList.Core;
using System.Linq;

namespace LaptopShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly laptopWebContext _context;
        public ProductController(laptopWebContext context)
        {
            _context = context;
        }

        [Route("shop.html", Name = "ShopProduct")]
        public IActionResult Index(int? page, int? pagesize, int? sort)
        {
            try
            {
                var pageNumber = page == null || page <= 0 ? 1 : page.Value;
                var pageSize = pagesize ?? 12;

                IQueryable<Product> lsProduct;
                if (sort == 0)
                {
                    lsProduct = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Image)
                    .Include(p => p.Info)
                    .OrderByDescending(x => x.ProductId);
                }
                else if (sort == 1)
                {
                    lsProduct = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Image)
                    .Include(p => p.Info)
                    .OrderBy(x => x.Price);
                }
                else
                {
                    lsProduct = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Image)
                    .Include(p => p.Info)
                    .OrderByDescending(x => x.Price);
                }
                var lsCategory = _context.Categories
                    .AsNoTracking()
                    .ToList();
                PagedList<Product> models = new PagedList<Product>(lsProduct, pageNumber, pageSize);
                ViewBag.CurrentPages = pageNumber;
                ViewBag.Categories = lsCategory;
                return View(models);
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult List(int id, int page = 1, int pagesize = 0, int sort = 0)
        {
            try
            {
                var pageSize = 12;
                if (pagesize != 0)
                {
                    pageSize = pagesize;
                }
                var danhmuc = _context.Categories
                    .AsNoTracking()
                    .SingleOrDefault(x => x.CategoryId == id);
                IQueryable<Product> lsPages;
                if (sort == 0)
                {
                    lsPages = _context.Products
                    .Include(p => p.Image)
                    .Include(p => p.Category)
                    .Include(p => p.Info)
                    .Where(p => p.CategoryId == id)
                    .AsNoTracking()
                    .OrderByDescending(x => x.Info.Date);
                }
                else if (sort == 1)
                {
                    lsPages = _context.Products
                    .Include(p => p.Image)
                    .Include(p => p.Category)
                    .Include(p => p.Info)
                    .Where(p => p.CategoryId == id)
                    .AsNoTracking()
                    .OrderBy(x => x.Price);
                }
                else
                {
                    lsPages = _context.Products
                    .Include(p => p.Image)
                    .Include(p => p.Category)
                    .Include(p => p.Info)
                    .Where(p => p.CategoryId == id)
                    .AsNoTracking()
                    .OrderByDescending(x => x.Price);
                }
                var lsCategory = _context.Categories
                    .AsNoTracking()
                    .ToList();

                PagedList<Product> models = new PagedList<Product>(lsPages, page, pageSize);
                ViewBag.CurrentPage = page;
                ViewBag.CurrentCat = danhmuc;
                ViewBag.Categories = lsCategory;
                return View(models);
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }

        //[Route("Products/{id}", Name ="Products")]
        public IActionResult Details(int id)
        {
            try
            {
                var product = _context.Products
                    .Include(p => p.Image)
                    .Include(p => p.Info)
                    .Include(p => p.Category)
                    .FirstOrDefault(x => x.ProductId == id);
                if (product == null)
                {
                    return RedirectToAction("Index");
                }

                var lsProduct = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Image)
                    .Include(p => p.Info)
                    .Include(p => p.Category)
                    .Where(x => x.CategoryId == product.CategoryId && x.ProductId != id)
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

        //[Route("products/{productname}", Name = "ProductDetails")]
        //public IActionResult Details(string productname)
        //{
        //    try
        //    {
        //        var product = _context.Products
        //            .Include(p => p.Image)
        //            .Include(p => p.Info)
        //            .Include(p => p.Category)
        //            .FirstOrDefault(x => x.ProductName == productname); // Giả sử ProductName là một định danh duy nhất
        //        if (product == null)
        //        {
        //            return RedirectToAction("Index");
        //        }

        //        var lsProduct = _context.Products
        //            .AsNoTracking()
        //            .Include(p => p.Image)
        //            .Include(p => p.Info)
        //            .Include(p => p.Category)
        //            .Where(x => x.CategoryId == product.CategoryId && x.ProductName != productname)
        //            .OrderByDescending(x => x.Price)
        //            .Take(4)
        //            .ToList();
        //        ViewBag.Sanpham = lsProduct;
        //        return View(product);
        //    }
        //    catch
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //}

    }
}
