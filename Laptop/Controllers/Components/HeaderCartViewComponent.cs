using System.Collections.Generic;
using System.Linq;
using LaptopShop.Extension;
using LaptopShop.Models;
using LaptopShop.ModelViews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopShop.Controllers.Components
{
    public class HeaderCartViewComponent : ViewComponent
    {
        private readonly laptopWebContext _context;

        public HeaderCartViewComponent(laptopWebContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var accountID = User.Identity.GetAccountID();

            if (int.TryParse(accountID, out var userId))
            {
                var cartItems = _context.Carts
                .Include(c => c.Product)
                .Include(c=> c.Product.Image)
                .Where(c => c.UserId == userId)
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
