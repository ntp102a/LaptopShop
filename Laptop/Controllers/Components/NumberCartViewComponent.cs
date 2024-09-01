using LaptopShop.Extension;
using LaptopShop.Models;
using LaptopShop.ModelViews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopShop.Controllers.Components
{
    public class NumberCartViewComponent : ViewComponent
    {
        private readonly laptopWebContext _context;

        public NumberCartViewComponent(laptopWebContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var accountID = User.Identity.GetAccountID();

            if (!string.IsNullOrEmpty(accountID))
            {
                var cartItems = _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == accountID)
                    .Select(c => new Cart
                    {
                        Product = c.Product,
                        Quantity = (int)c.Quantity
                    })
                    .ToList();

                return View(cartItems);
            }
            else
            {
                return View(new List<Cart>());
            }
        }
    }
}
