using AspNetCoreHero.ToastNotification.Abstractions;
using BraintreeHttp;
using LaptopShop.Extension;
using LaptopShop.Models;
using LaptopShop.ModelViews;
using LaptopShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PayPal.Core;
using PayPal.v1.Payments;
using System.Diagnostics;
using System.Security.Claims;

namespace LaptopShop.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly laptopWebContext _context;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly IConfiguration _configuration;
        public double TyGiaUSD = 24535;
        private readonly IVnPayService _vnPayservice;
        public INotyfService _notyfService { get; }
        public CheckoutController(laptopWebContext context, INotyfService notyfService, IConfiguration config, IVnPayService vnPayservice, IConfiguration configuration)
        {
            _context = context;
            _notyfService = notyfService;
            _clientId = config["PayPalSettings:clientId"];
            _clientSecret = config["PayPalSettings:clientSecret"];
            _vnPayservice = vnPayservice;
            _configuration = configuration;
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
        [Route("/thanh-toan", Name = "Checkout")]
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

                return RedirectToAction("Login", "Accounts");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("/thanh-toan", Name = "Index")]
        public IActionResult Index(MuaHangVM muahang)
        {
            try
            {
                // Lưu dữ liệu vào TempData trước khi chuyển hướng đến VNPay
                TempData["FullName"] = muahang.FullName;
                TempData["Address"] = muahang.Address;
                TempData["Phone"] = muahang.Phone;
                TempData["Note"] = muahang.Note;
                TempData["Email"] = muahang.Email;

                string paymentMethod = Request.Form["PaymentMethod"];
                if (paymentMethod == "VnPay")
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
                        StatusId = 1,
                        IsPayment = false,
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

                return RedirectToAction("Login", "Accounts");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        //[Authorize]
        //public IActionResult PaymentCallBack()
        //{
        //    var response = _vnPayservice.PaymentExecute(Request.Query);

        //    if (response == null || response.VnPayResponseCode != "00")
        //    {
        //        TempData["Message"] = $"Lỗi thanh toán VN Pay: {response.VnPayResponseCode}";
        //        return RedirectToAction("Fail");
        //    }

        //    // Lưu đơn hàng vô database

        //    TempData["Message"] = $"Thanh toán VNPay thành công";
        //    return RedirectToAction("Success");
        //}
        [Authorize]
        [Route("/Callback")]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayservice.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Lỗi thanh toán VN Pay: {response.VnPayResponseCode}";
                return RedirectToAction("Fail");
            }

            // Lấy accountID của người dùng
            var accountID = User.Identity.GetAccountID();
            if (!string.IsNullOrEmpty(accountID))
            {
                // Lấy thông tin giỏ hàng của người dùng
                var cart = _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == accountID)
                    .ToList();

                // Tạo đơn hàng mới
                var donhang = new Models.Order
                {
                    UserId = accountID,
                    RecipientName = TempData["FullName"]?.ToString(),
                    Address = TempData["Address"]?.ToString(),
                    Phone = TempData["Phone"]?.ToString(),
                    OrderDate = DateTime.Now,
                    Note = TempData["Note"]?.ToString(),
                    StatusId = 1,
                    IsPayment = true,
                    Total = Convert.ToInt32(cart.Sum(x => x.TotalMoney))
                };

                _context.Add(donhang);
                _context.SaveChanges();

                // Thêm chi tiết đơn hàng cho từng sản phẩm trong giỏ hàng
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

                    // Cập nhật số lượng tồn kho của sản phẩm
                    var product = _context.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        product.Instock -= item.Quantity;
                        _context.Products.Update(product);
                    }
                }

                // Lưu tất cả thay đổi vào cơ sở dữ liệu
                _context.SaveChanges();

                // Hiển thị thông báo thành công
                TempData["Message"] = "Thanh toán VNPay thành công";
                return RedirectToAction("Success");
            }

            TempData["Message"] = "Có lỗi xảy ra trong quá trình xử lý thanh toán.";
            return RedirectToAction("Fail");
        }



        private string GenerateOrderTableHtml(List<dynamic> orderDetails)
        {
            string table = "<table border='1' style='border-collapse: collapse; width: 100%;'>";
            table += "<thead><tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>";

            foreach (var item in orderDetails)
            {
                table += $"<tr><td>{item.ProductName}</td><td>{item.Quantity}</td><td>{item.Price:C}</td><td>{item.Total:C}</td></tr>";
            }

            table += "</tbody></table>";
            return table;
        }


        [Route("dat-hang-thanh-cong", Name = "Success")]
        public IActionResult Success()
        {
            try
            {
                var accountID = User.Identity.GetAccountID();
                var email = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;

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
                            Address = donhang.Address,
                        };

                        var orderDetails = _context.OrderDetails
                            .Where(od => od.OrderId == donhang.OrderId)
                            .Select(od => new
                            {
                                od.Product.ProductName,
                                od.Quantity,
                                od.Price,
                                Total = od.Quantity * od.Price
                            })
                            .ToList()
                            .Select(item => (dynamic)item)
                            .ToList();



                        string orderTableHtml = GenerateOrderTableHtml(orderDetails);

                        // Nội dung email
                        string subject = $"Đơn hàng #{donhang.OrderId} của bạn đã được đặt thành công";
                        string body = $"<h3>Xin chào {successVM.FullName},</h3>" +
                                      $"<p>Cảm ơn bạn đã mua sắm tại cửa hàng chúng tôi. Dưới đây là thông tin chi tiết đơn hàng:</p>" +
                                      $"{orderTableHtml}" +
                                      $"<p>Chúng tôi sẽ liên hệ với bạn qua số điện thoại {successVM.Phone}.</p>" +
                                      "<p>Trân trọng,</p>" +
                                      "<p>Đội ngũ hỗ trợ khách hàng.</p>";

                        // Gửi email
                        EmailService emailService = new EmailService(_configuration);
                        emailService.SendEmailAsync(email, subject, body);


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

        [Route("dat-hang-khong-thanh-cong", Name = "Fail")]
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
                        CancelUrl = $"{hostname}/dat-hang-khong-thanh-cong",
                        ReturnUrl = $"{hostname}/dat-hang-thanh-cong"
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
                            StatusId = 1,
                            IsPayment = true,
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

                return Redirect("/dat-hang-khong-thanh-cong");
            }
        }
    }
}
