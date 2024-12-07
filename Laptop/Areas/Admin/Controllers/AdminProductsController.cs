using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LaptopShop.Models;
using AspNetCoreHero.ToastNotification.Abstractions;
using PagedList.Core;
using LaptopShop.Helpper;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace LaptopShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class AdminProductsController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }

        public AdminProductsController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        // GET: Admin/AdminProducts
        public IActionResult Index(int page = 1, int category_id = 0)
        {
            var pageNumber = page;
            var pageSize = 5;
            List<Product> IsProducts = new List<Product>();

            // Lưu giá trị category_id vào ViewBag để sử dụng sau này
            ViewBag.CurrentCateID = category_id;

            if (category_id != 0)
            {
                IsProducts = _context.Products
                    .AsNoTracking()
                    .Where(x => x.CategoryId == category_id && x.IsPublic == true)
                    .Include(x => x.Category)
                    .OrderByDescending(x => x.ProductId)
                    .ToList();
            }
            else
            {
                IsProducts = _context.Products
                    .AsNoTracking()
                    .Include(x => x.Category)
                    .Where(x => x.IsPublic == true)
                    .OrderByDescending(x => x.ProductId)
                    .ToList();
            }

            PagedList<Product> models = new PagedList<Product>(IsProducts.AsQueryable(), pageNumber, pageSize);

            ViewBag.CurrentPage = pageNumber;

            ViewData["DanhMuc"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", category_id);
            return View(models);
        }

        public IActionResult Filtter(int CatID)
        {
            var url = $"/Admin/AdminProducts?category_id={CatID}";
            if (CatID == 0)
            {
                url = $"/Admin/AdminProducts";
            }

            return Json(new { status = "success", RedirectUrl = url });
        }

        // GET: Admin/AdminProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Image)
                .Include(p => p.Info)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/AdminProducts/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            ViewData["ImageId"] = new SelectList(_context.Images, "ImageId", "ImageId");
            ViewData["InfoId"] = new SelectList(_context.Information, "InfoId", "InfoId");
            return View();
        }

        // POST: Admin/AdminProducts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, Information information,
            Image image, IFormFile fThumb, IFormFile fimage1, IFormFile fimage2, IFormFile fimage3)
        {
            product.ProductName = Utilities.ToTitleCase(product.ProductName);

            if (fThumb != null)
            {
                string extension = Path.GetExtension(fThumb.FileName);
                string images = Utilities.SEOUrl(product.ProductName) + extension;

                image.ImageThumb = await Utilities.UploadFile(fThumb, @"products", images.ToLower());
            }

            if (fimage1 != null)
            {
                string extension = Path.GetExtension(fimage1.FileName);
                string images = Utilities.SEOUrl(product.ProductName) + "_small1" + extension;

                image.Image1 = await Utilities.UploadFile(fimage1, @"products", images.ToLower());
            }

            if (fimage2 != null)
            {
                string extension = Path.GetExtension(fimage2.FileName);
                string images = Utilities.SEOUrl(product.ProductName) + "_small2" + extension;

                image.Image2 = await Utilities.UploadFile(fimage2, @"products", images.ToLower());
            }

            if (fimage3 != null)
            {
                string extension = Path.GetExtension(fimage3.FileName);
                string images = Utilities.SEOUrl(product.ProductName) + "_small3" + extension;

                image.Image3 = await Utilities.UploadFile(fimage3, @"products", images.ToLower());
            }

            if (string.IsNullOrEmpty(image.ImageThumb)) image.ImageThumb = "default.jpg";
            if (string.IsNullOrEmpty(image.Image1)) image.Image1 = "default.jpg";
            if (string.IsNullOrEmpty(image.Image2)) image.Image2 = "default.jpg";
            if (string.IsNullOrEmpty(image.Image3)) image.Image3 = "default.jpg";

            // Thêm thông tin sản phẩm vào cơ sở dữ liệu
            information = product.Info;
            _context.Information.Add(information);
            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            product.InfoId = information.InfoId;
            product.ImageId = image.ImageId;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            _notyfService.Success("Thêm mới thành công");
            return RedirectToAction(nameof(Index));
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(product);
        }

        // GET: Admin/AdminProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Image)
                .Include(p => p.Info)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            //var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewData["ImageId"] = new SelectList(_context.Images, "ImageId", "ImageThumb", product.ImageId);
            ViewData["InfoId"] = new SelectList(_context.Information, "InfoId", "InfoId", product.InfoId);
            return View(product);
        }

        // POST: Admin/AdminProducts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, Information information, Image image, IFormFile fThumb, IFormFile fimage1, IFormFile fimage2, IFormFile fimage3)
        {
            if (id != product.ProductId && image.ImageId != product.ImageId && information.InfoId != product.InfoId)
            {
                return NotFound();
            }

            try
            {
                product.ProductName = Utilities.ToTitleCase(product.ProductName);

                if (fThumb != null)
                {
                    string extension = Path.GetExtension(fThumb.FileName);
                    string images = Utilities.SEOUrl(product.ProductName) + extension;

                    image.ImageThumb = await Utilities.UploadFile(fThumb, @"products", images.ToLower());
                }

                if (fimage1 != null)
                {
                    string extension = Path.GetExtension(fimage1.FileName);
                    string images = Utilities.SEOUrl(product.ProductName) + "_small1" + extension;

                    image.Image1 = await Utilities.UploadFile(fimage1, @"products", images.ToLower());
                }

                if (fimage2 != null)
                {
                    string extension = Path.GetExtension(fimage2.FileName);
                    string images = Utilities.SEOUrl(product.ProductName) + "_small2" + extension;

                    image.Image2 = await Utilities.UploadFile(fimage2, @"products", images.ToLower());
                }

                if (fimage3 != null)
                {
                    string extension = Path.GetExtension(fimage3.FileName);
                    string images = Utilities.SEOUrl(product.ProductName) + "_small3" + extension;

                    image.Image3 = await Utilities.UploadFile(fimage3, @"products", images.ToLower());
                }

                information = product.Info;
                information.InfoId = (int)product.InfoId;
                _context.Information.Update(information);
                if (!string.IsNullOrEmpty(image.ImageThumb) && !string.IsNullOrEmpty(image.Image1) && !string.IsNullOrEmpty(image.Image2) && !string.IsNullOrEmpty(image.Image3))
                {
                    _context.Images.Update(image);
                }    
                
                _context.Products.Update(product);
                await _context.SaveChangesAsync();


                _notyfService.Success("Sửa thành công");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.ProductId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/AdminProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Image)
                .Include(p => p.Info)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/AdminProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Products == null)
            {
                return Problem("Entity set 'laptopWebContext.Products'  is null.");
            }
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsPublic = false;
                _context.Update(product);
            }

            //var info = await _context.Information.FindAsync(product.InfoId);
            //if (info != null)
            //{
            //    _context.Information.Remove(info);
            //}

            //var image = await _context.Images.FindAsync(product.ImageId);
            //if (image != null)
            //{
            //    _context.Images.Remove(image);
            //}

            var cart = _context.Carts.Include(x => x.Product).Where(x => x.ProductId == id).ToList();
            if (cart != null)
            {
                foreach (var item in cart)
                {
                    _context.Carts.Remove(item);
                }
            }

            //var order = _context.OrderDetails.Include(x => x.Product).Where(x => x.ProductId == id).ToList();
            //if (order != null)
            //{
            //    foreach (var item in order)
            //    {
            //        _context.OrderDetails.Remove(item);
            //    }
            //}

            await _context.SaveChangesAsync();
            _notyfService.Success("Xoá sản phẩm thành công");
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return (_context.Products?.Any(e => e.ProductId == id)).GetValueOrDefault();
        }
    }
}
