using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Models;
using LaptopShop.ModelViews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LaptopShop.Controllers
{
    public class DonHangController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }
        public DonHangController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        //GET: DonHang/Details/5
        [HttpPost]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            try
            {
                var taikhoanID = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(taikhoanID)) return RedirectToAction("Login", "Accounts");
                var khachhang = _context.Users
                    .AsNoTracking()
                    .SingleOrDefault(x => x.UserId == taikhoanID);

                if (khachhang == null) return NotFound();
                var donhang= await _context.Orders
                    .Include(x => x.Status.Status)
                    .FirstOrDefaultAsync(x => x.OrderId== id && taikhoanID == x.UserId);
                if (donhang == null) return NotFound();

                var chitietdonhang = _context.OrderDetails
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Where(x => x.OrderId == id)
                    .OrderBy(x => x.OrderDetailId)
                    .ToList();
                XemDonHang donHang = new XemDonHang();
                donHang.DonHang = donhang;
                donHang.ChiTietDonHang = chitietdonhang;
                return PartialView("Details", donHang);
            }
            catch
            {
                return NotFound();
            }
            
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
