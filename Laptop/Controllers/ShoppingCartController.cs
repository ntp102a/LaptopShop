using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopShop.Models;
using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Extension;
using Microsoft.AspNetCore.Authorization;

namespace LaptopShop.Controllers
{
    [Authorize(Roles = "2")]
    public class ShoppingCartController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }
        public ShoppingCartController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        [HttpPost]
        [Route("api/cart/add")]
        public IActionResult AddtoCart(int productID, int? amount)
        {
            try
            {
                var accountID = User.Identity.GetAccountID();

                if (string.IsNullOrEmpty(accountID))
                {
                    return Unauthorized(new
                    {
                        result = "Redirect",
                        message = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.",
                        url = Url.Action("Login", "Accounts", new { returnUrl = Request.Path })
                    });
                }

                var cartItem = _context.Carts
                    .FirstOrDefault(c => c.UserId == accountID && c.ProductId == productID);

                if (cartItem != null)
                {
                    cartItem.Quantity += amount ?? 1;
                }
                else
                {
                    var newCartItem = new Cart
                    {
                        UserId = accountID,
                        ProductId = productID,
                        Quantity = amount ?? 1
                    };

                    _context.Carts.Add(newCartItem);
                }

                _context.SaveChanges();
                return Json(new
                {
                    result = "Success",
                    message = "Thêm vào giỏ hàng thành công"
                });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("api/cart/update")]
        public IActionResult UpdateCart(int productID, int? amount)
        {
            try
            {
                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var cartItem = _context.Carts
                        .FirstOrDefault(c => c.UserId == accountID && c.ProductId == productID);

                    if (cartItem != null && amount.HasValue)
                    {
                        cartItem.Quantity = amount.Value;
                        _context.SaveChanges();
                        return Json(new { success = true });
                    }
                }

                return Json(new { success = false, error = "Lỗi khi cập nhật giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("api/cart/remove")]
        public IActionResult Remove(int productID)
        {
            try
            {
                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var cartItem = _context.Carts
                        .FirstOrDefault(c => c.UserId == accountID && c.ProductId == productID);

                    if (cartItem != null)
                    {
                        _context.Carts.Remove(cartItem);
                        _context.SaveChanges();
                        return Json(new { success = true });
                    }
                }

                return Json(new { success = false, error = "Lỗi khi xóa sản phẩm khỏi giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [Route("/gio-hang", Name = "Cart")]
        public IActionResult Index()
        {
            var userId = User.Identity.GetAccountID();
            var gioHang = _context.Carts
                .Include(c => c.Product)
                .Include(c => c.User)
                .Where(c => c.UserId == userId).ToList();
            var cartItems = gioHang.Select(c => new Cart
            {
                Product = c.Product,
                Quantity = c.Quantity,
                User= c.User,
            }).ToList();

            return View(cartItems);
        }
    }
}
