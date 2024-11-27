using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace LaptopShop.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }
        public ReviewsController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }
        [HttpPost]
        public IActionResult Create(Review model)
        {
            var taikhoanID = HttpContext.Session.GetString("UserId");
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


            return RedirectToAction("Index", new { productId = model.ProductId });
        }
    }
}
