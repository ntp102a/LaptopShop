using AspNetCoreHero.ToastNotification.Abstractions;
using BraintreeHttp;
using LaptopShop.Extension;
using LaptopShop.Models;
using LaptopShop.ModelViews;
using LaptopShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal.Core;
using PayPal.v1.Payments;
using System.Diagnostics;

namespace LaptopShop.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly laptopWebContext _context;
        private readonly string _clientId;
        private readonly string _clientSecret;
        public double TyGiaUSD = 24535;
        private readonly IVnPayService _vnPayservice;
        public INotyfService _notyfService { get; }
        public CheckoutController(laptopWebContext context, INotyfService notyfService, IConfiguration config, IVnPayService vnPayservice)
        {
            _context = context;
            _notyfService = notyfService;
            _clientId = config["PayPalSettings:clientId"];
            _clientSecret = config["PayPalSettings:clientSecret"];
            _vnPayservice = vnPayservice;
        }

        public List<Cart> GioHang
        {
            get
            {
                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var gioHang = _context.Carts
                        .Include(c => c.Product)
                        .Include(c => c.User)
                        .Where(c => c.UserId == accountID)
                        .ToList();

                    var cartItems = gioHang.Select(c => new Cart
                    {
                        Product = c.Product,
                        Quantity = c.Quantity,
                        User = c.User,
                    }).ToList();

                    if (cartItems == default(List<Cart>))
                    {
                        cartItems = new List<Cart>();
                    }

                    return cartItems;
                }

                return new List<Cart>();
            }
        }

        //GET: Checkout/Index
        [Authorize]
        [HttpGet]
        [Route("checkout.html", Name = "Checkout")]
        public IActionResult Index()
        {
            try
            {
                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var model = new MuaHangVM();
                    var khachhang = _context.Users
                        .AsNoTracking()
                        .SingleOrDefault(x => x.UserId == accountID);

                    if (khachhang != null)
                    {
                        model.UserId = khachhang.UserId;
                        model.FullName = khachhang.FullName;
                        model.Email = khachhang.Email;
                        model.Phone = khachhang.Phone;
                        model.Address = khachhang.Address;
                    }

                    // Lấy giỏ hàng từ CSDL
                    var cart = _context.Carts
                        .Include(c => c.Product)
                        .Where(c => c.UserId == accountID)
                        .Select(c => new Cart
                        {
                            Product = c.Product,
                            Quantity = (int)c.Quantity
                        })
                        .ToList();

                    ViewBag.GioHang = cart;
                    return View(model);
                }

                return RedirectToAction("Login", "Account"); // Chuyển hướng đến trang đăng nhập nếu không có tài khoản
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("checkout.html", Name = "Index")]
        public IActionResult Index(MuaHangVM muahang)
        {
            try
            {
                string paymentMethod = Request.Form["PaymentMethod"];
                if(paymentMethod == "VnPay")
                {
                    var total = 0.0;
                    foreach (var item in GioHang)
                    {
                        var totalProduct = item.Product.Price * item.Quantity;
                        total += (double)totalProduct;
                    }

                    var vnPayModel = new VnPaymentRequestModel
                    {
                        Amount = total,
                        CreatedDate = DateTime.Now,
                        Description = $"{muahang.FullName} {muahang.Phone}",
                        FullName = muahang.FullName,
                        OrderId = new Random().Next(1000, 100000)
                    };

                    return Redirect(_vnPayservice.CreatePaymentUrl(HttpContext, vnPayModel));
                }

                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var cart = _context.Carts
                        .Include(c => c.Product)
                        .Where(c => c.UserId == accountID)
                        .ToList();

                    var khachhang = _context.Users
                        .SingleOrDefault(x => x.UserId == accountID);

                    if (khachhang.Address == null)
                    {
                        khachhang.Address = muahang.Address;
                        _context.Update(khachhang);
                        _context.SaveChanges();
                    }

                    var donhang = new Models.Order
                    {
                        UserId = accountID,
                        RecipientName = muahang.FullName,
                        Address = muahang.Address,
                        Phone = muahang.Phone,
                        OrderDate = DateTime.Now,
                        Note = muahang.Note,
                        StatusId = 6,
                        Total = Convert.ToInt32(cart.Sum(x => x.TotalMoney))
                    };

                    _context.Add(donhang);
                    _context.SaveChanges();

                    foreach (var item in cart)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = donhang.OrderId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.Product.Price
                        };

                        _context.Add(orderDetail);

                        var product = _context.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            product.Instock -= item.Quantity;
                            _context.Products.Update(product);
                        }
                    }

                    _context.SaveChanges();
                    HttpContext.Session.Remove("GioHang");
                    _notyfService.Success("Đặt hàng thành công");
                    return RedirectToAction("Success");
                }

                return RedirectToAction("Login", "Account"); // Chuyển hướng đến trang đăng nhập nếu không có tài khoản
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [Route("dat-hang-thanh-cong.html", Name = "Success")]
        public IActionResult Success()
        {
            try
            {
                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var donhang = _context.Orders
                        .Where(x => x.UserId == accountID)
                        .OrderByDescending(x => x.OrderDate)
                        .FirstOrDefault();

                    if (donhang != null)
                    {
                        MuaHangSuccessVM successVM = new MuaHangSuccessVM
                        {
                            FullName = donhang.RecipientName,
                            DonHangID = donhang.OrderId,
                            Phone = donhang.Phone,
                            Address = donhang.Address
                        };

                        var cartItems = _context.Carts
                            .Where(c => c.UserId == accountID)
                            .ToList();

                        _context.Carts.RemoveRange(cartItems);
                        _context.SaveChanges();

                        return View(successVM);
                    }
                }
                _notyfService.Error("Đặt hàng không thành công");
                return RedirectToAction("Login", "Accounts", new { returnUrl = "/dat-hang-thanh-cong.html" });
            }
            catch
            {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [Route("dat-hang-khong-thanh-cong.html", Name = "Fail")]
        public IActionResult Fail()
        {
            //Tạo đơn hàng trong database với trạng thái thanh toán là "Chưa thanh toán"
            //Xóa session
            return View();
        }

        [Authorize]
        public async Task<IActionResult> PaypalCheckout()
        {
            var environment = new SandboxEnvironment(_clientId, _clientSecret);
            var client = new PayPalHttpClient(environment);

            try
            {
                var itemList = new ItemList()
                {
                    Items = new List<Item>()
                };

                //var total = Math.Round(GioHang.Sum(p => p.TotalMoney) / TyGiaUSD, 2);
                var total = 0.0;
                foreach (var item in GioHang)
                {
                    var totalProduct = Math.Round((decimal)(item.Product.Price / TyGiaUSD), 2) * item.Quantity;
                    total += (double)totalProduct;
                }

                foreach (var item in GioHang)
                {
                    itemList.Items.Add(new Item()
                    {
                        Name = item.Product.ProductName,
                        Currency = "USD",
                        Quantity = item.Quantity.ToString(),
                        Sku = "sku",
                        Tax = "0",
                        Price = Math.Round(item.Product.Price.Value / TyGiaUSD, 2).ToString()
                    });
                }

                var paypalOrderId = DateTime.Now.Ticks;
                var hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

                var payment = new Payment()
                {
                    Intent = "sale",
                    Transactions = new List<Transaction>()
            {
                new Transaction()
                {
                    Amount = new Amount()
                    {
                        Total = total.ToString(),
                        Currency = "USD",
                        Details = new AmountDetails
                        {
                            Tax = "0",
                            Shipping = "0",
                            Subtotal = total.ToString(),
                        }
                    },
                    ItemList = itemList,
                    Description = $"Invoice #{paypalOrderId}",
                    InvoiceNumber = paypalOrderId.ToString()
                }
            },
                    RedirectUrls = new RedirectUrls()
                    {
                        CancelUrl = $"{hostname}/dat-hang-khong-thanh-cong.html",
                        ReturnUrl = $"{hostname}/dat-hang-thanh-cong.html"
                    },
                    Payer = new Payer()
                    {
                        PaymentMethod = "paypal"
                    }
                };

                PaymentCreateRequest request = new PaymentCreateRequest();
                request.RequestBody(payment);

                var response = await client.Execute(request);
                var statusCode = response.StatusCode;
                Payment result = response.Result<Payment>();

                var links = result.Links.GetEnumerator();
                string paypalRedirectUrl = null;

                while (links.MoveNext())
                {
                    LinkDescriptionObject lnk = links.Current;
                    if (lnk.Rel.ToLower().Trim().Equals("approval_url"))
                    {
                        paypalRedirectUrl = lnk.Href;
                    }
                }
                #region Update vào csdl
                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var khachhang = _context.Users
                        .AsNoTracking()
                        .SingleOrDefault(x => x.UserId == accountID);

                    var cartItems = _context.Carts
                        .Include(c => c.Product)
                        .Where(c => c.UserId == accountID)
                        .ToList();

                    if (cartItems.Any())
                    {
                        var donhang = new Models.Order
                        {
                            UserId = accountID,
                            Address = khachhang?.Address,
                            Phone = khachhang?.Phone,
                            RecipientName = khachhang.FullName,
                            OrderDate = DateTime.Now,
                            StatusId = 4,
                            Total = Convert.ToInt32(cartItems.Sum(x => x.TotalMoney))
                        };

                        _context.Orders.Add(donhang);
                        _context.SaveChanges();

                        foreach (var item in cartItems)
                        {
                            var orderDetail = new OrderDetail
                            {
                                OrderId = donhang.OrderId,
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                Price = item.Product.Price
                            };

                            _context.OrderDetails.Add(orderDetail);

                            var product = _context.Products.Find(item.ProductId);
                            product.Instock -= item.Quantity;
                            _context.Products.Update(product);
                        }

                        _context.Carts.RemoveRange(cartItems);
                        _context.SaveChanges();
                    }
                }
                #endregion

                return Redirect(paypalRedirectUrl);
            }
            catch (HttpException httpException)
            {
                var statusCode = httpException.StatusCode;
                var debugId = httpException.Headers.GetValues("PayPal-Debug-Id").FirstOrDefault();

                return Redirect("/dat-hang-khong-thanh-cong.html");
            }
        }

        [Authorize]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayservice.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Lỗi thanh toán VN Pay: {response.VnPayResponseCode}";
                return RedirectToAction("Fail");
            }

            // Lưu đơn hàng vô database

            TempData["Message"] = $"Thanh toán VNPay thành công";
            return RedirectToAction("Success");
        }
    }
}
