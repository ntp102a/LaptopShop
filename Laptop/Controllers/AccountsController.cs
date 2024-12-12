using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Helpper;
using LaptopShop.Extension;
using LaptopShop.Models;
using LaptopShop.ModelViews;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using LaptopShop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace LaptopShop.Controllers
{
    public class AccountsController : Controller
    {
        private readonly laptopWebContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        public INotyfService _notyfService { get; }
        public AccountsController(laptopWebContext context, INotyfService notyfService, IConfiguration configuration, IMemoryCache cache)
        {
            _context = context;
            _notyfService = notyfService;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ValidatePhone(string Phone)
        {
            try
            {
                var khachhang = _context.Users
                    .AsNoTracking()
                    .SingleOrDefault(x => x.Phone.ToLower() == Phone.ToLower());
                if (khachhang != null)
                {
                    return Json(data: "Số điện thoại : " + Phone + " đã được đăng ký");
                }
                return Json(data: true);
            }
            catch
            {
                return Json(data: true);
            }
        }

        private string GenerateRandomId()
        {
            return Guid.NewGuid().ToString();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ValidateEmail(string Email)
        {
            try
            {
                var khachhang = _context.Users
                    .AsNoTracking()
                    .SingleOrDefault(x => x.Email.ToLower() == Email.ToLower());
                if (khachhang != null)
                {
                    return Json(data: "Email : " + Email + " đã được đăng ký");
                }
                return Json(data: true);
            }
            catch
            {
                return Json(data: true);
            }
        }

        [Authorize]
        [Route("/tai-khoan-cua-toi", Name = "Dashboard")]
        public IActionResult Dashboard()
        {
            var accountID = User?.Identity?.GetAccountID();
            if (accountID != null)
            {
                var user = _context.Users.FirstOrDefault(p => p.UserId == accountID);
                return View(user);
            }

            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult GetDashboardInfo(string title, int orderId)
        {
            var taikhoanID = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (taikhoanID != null)
            {
                switch (title)
                {
                    case "Thông tin tài khoản":
                        var khachhang = _context.Users.AsNoTracking().SingleOrDefault(x => x.UserId == taikhoanID);
                        return PartialView("_InformationPartialView", khachhang);
                    case "Danh sách đơn hàng":
                        var listOrder = _context.Orders
                         .Include(x => x.Status)
                         .AsNoTracking()
                         .Where(x => x.UserId == taikhoanID)
                         .OrderByDescending(x => x.OrderDate).ToList();
                        return PartialView("_DonhangPartialView", listOrder);
                    case "Thay đổi thông tin":
                        var userInfo = _context.Users
                            .AsNoTracking()
                            .FirstOrDefault(x => x.UserId == taikhoanID);

                        if (userInfo == null)
                        {
                            return BadRequest("Không tìm thấy dữ liệu phù hợp.");
                        }
                        else
                        {
                            var modelView = new ChangeInfoViewModel
                            {
                                FullName = userInfo.FullName,
                                Address = userInfo.Address,
                                Phone = userInfo.Phone
                            };
                            return PartialView("_ChangeInfoPartialView", modelView);
                        }
                    case "Đổi mật khẩu":
                        return PartialView("_ChangePasswordPartialView");
                    case "Xem chi tiết":
                        var orderDetail = _context.Orders.Include(x => x.Status).Include(x => x.User).Include(x => x.OrderDetails).ThenInclude(x => x.Product).FirstOrDefault(x => x.OrderId == orderId);
                        return PartialView("_OrderDetailsPartialView", orderDetail);
                    default:
                        return BadRequest("Không tìm thấy dữ liệu phù hợp.");
                }
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult ChangeInfo(ChangeInfoViewModel model)
        {
            var accountID = User?.Identity?.GetAccountID();
            if (string.IsNullOrEmpty(accountID))
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(p => p.UserId == accountID);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (!string.IsNullOrWhiteSpace(model.Phone) && model.Phone != user.Phone)
            {
                var checkPhone = _context.Users.FirstOrDefault(x => x.Phone.ToLower() == model.Phone.ToLower());
                if (checkPhone != null)
                {
                    _notyfService.Error("Số điện thoại đã tồn tại");
                    return RedirectToAction("Dashboard", "Accounts");
                }
            }

            user.FullName = model.FullName;
            user.Address = model.Address;
            user.Phone = model.Phone;

            _context.Update(user);
            try
            {
                _context.SaveChanges();
                _notyfService.Success("Thay đổi thông tin thành công");
                return RedirectToAction("Dashboard", "Accounts");
            }
            catch
            {
                _notyfService.Error("Thay đổi không thành công");
                return RedirectToAction("Dashboard", "Accounts");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User?.Identity?.GetAccountID();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _notyfService.Error("Không tìm thấy học viên");
                return RedirectToAction("Dashboard");
            }

            // Kiểm tra mật khẩu cũ
            var currentPasswordHash = (model.OldPassword + user.Salt).ToMD5();
            if (user.Password != currentPasswordHash)
            {
                _notyfService.Success("Sai mật khẩu");
                return RedirectToAction("Dashboard");
            }

            // Cập nhật mật khẩu mới
            string newSalt = Utilities.GetRandomKey();
            user.Salt = newSalt;
            user.Password = (model.NewPassword + newSalt).ToMD5();

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
                _notyfService.Success("Đổi mật khẩu thành công.");
            }
            catch (Exception)
            {
                _notyfService.Error("Có lỗi xảy ra khi đổi mật khẩu.");
            }

            return RedirectToAction("Dashboard");
        }

        [Authorize]
        [HttpPost]
        public IActionResult CancelOrder(int orderId)
        {
            try
            {
                var accountId = User?.Identity?.GetAccountID();
                if (string.IsNullOrEmpty(accountId))
                {
                    return RedirectToAction("Login");
                }

                // Lấy thông tin đơn hàng theo orderId
                var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == accountId);
                if (order == null)
                {
                    _notyfService.Error("Đơn hàng không tồn tại hoặc bạn không có quyền hủy đơn này.");
                    return RedirectToAction("Dashboard");
                }

                // Cập nhật trạng thái đơn hàng
                order.StatusId = 4;
                _context.Update(order);
                _context.SaveChanges();

                _notyfService.Success("Đơn hàng đã được hủy thành công.");

                return RedirectToAction("GetDashboardInfo", new {title = "Danh sách đơn hàng" });
            }
            catch (Exception ex)
            {
                _notyfService.Error("Đã có lỗi xảy ra khi hủy đơn hàng.");
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("/dang-ky", Name = "DangKy")]
        public IActionResult DangkyTaiKhoan()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [Route("/dang-ky", Name = "DangKy")]
        public async Task<IActionResult> DangkyTaiKhoan(RegisterVM taikhoan)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingUser = _context.Users.SingleOrDefault(x => x.Email.ToLower() == taikhoan.Email.ToLower());
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "Email đã tồn tại");
                        return View(taikhoan);
                    }

                    string salt = Utilities.GetRandomKey();
                    string verificationCode = new Random().Next(100000, 999999).ToString();
                    SaveVerificationCode(taikhoan.Email, verificationCode);

                    User khachhang = new User
                    {
                        UserId = Guid.NewGuid().ToString(),
                        FullName = taikhoan.Fullname,
                        Phone = taikhoan.Phone.Trim().ToLower(),
                        Email = taikhoan.Email.Trim().ToLower(),
                        Password = (taikhoan.Password + salt.Trim()).ToMD5(),
                        Salt = salt,
                        RoleId = 2,
                        IsVerified = false,
                    };
                    try
                    {
                        _context.Add(khachhang);
                        await _context.SaveChangesAsync();

                        //var UrlVerifiAccount = Url.Action("VerifyAccount", "Accounts", new { userId = khachhang.UserId }, Request.Scheme);

                        //Luu session UserId
                        HttpContext.Session.SetString("Email", taikhoan.Email);
                        //Gửi email xác nhận
                        string subject = "Xác nhận tài khoản đăng ký";
                        string body = $"<p>Xin chào {khachhang.FullName},</p>" +
                                      $"<p>Bạn đã đăng ký tài khoản tại hệ thống của chúng tôi. Đây là mã xác nhận tài khoản của bạn:</p>" +
                                      $"<p>Mã xác nhận: {verificationCode}</p>" +
                                      $"<p>Trân trọng,<br>Đội ngũ hỗ trợ</p>";

                        EmailService emailService = new EmailService(_configuration);
                        await emailService.SendEmailAsync(khachhang.Email, subject, body);
                        _notyfService.Success("Đăng ký thành công. Vui lòng kiểm tra email.");

                        //return RedirectToAction("Login", "Accounts");
                        TempData["Email"] = taikhoan.Email;
                        return RedirectToAction("VerifyEmail", new { email = khachhang.Email });
                    }
                    catch
                    {
                        return RedirectToAction("DangKyTaiKhoan", "Accounts");
                    }
                }
                else
                {
                    return View(taikhoan);
                }
            }
            catch
            {
                return View(taikhoan);
            }
        }

        public void SaveVerificationCode(string email, string code)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            _cache.Set(email, code, cacheEntryOptions);
        }
        public bool VerifyCode(string email, string enteredCode)
        {
            if (_cache.TryGetValue(email, out string storedCode))
            {
                return storedCode == enteredCode;
            }

            return false;
        }

        [AllowAnonymous]
        [Route("xac-thuc-tai-khoan/{email}")]
        public IActionResult VerifyEmail(string email)
        {
            ViewBag.Email = email;
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("xac-thuc-tai-khoan/{email}")]
        public async Task<IActionResult> VerifyEmail(string email, string code)
        {
            if (email == null)
            {
                _notyfService.Error("Không tìm thấy Email");
                return RedirectToAction("DangKyTaiKhoan", "Accounts");
            }
            if (VerifyCode(email, code))
            {

                var taikhoan = _context.Users.FirstOrDefault(p => p.Email == email);
                if (taikhoan == null)
                {
                    _notyfService.Error("Không tìm thấy tài khoản");
                    return RedirectToAction("DangKyTaiKhoan", "Accounts");
                }
                else
                {
                    taikhoan.IsVerified = true;
                    _context.Update(taikhoan);
                    _context.SaveChanges();
                }

                // Tạo Claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, taikhoan.FullName),
                    new Claim("UserId", taikhoan.UserId),
                    new Claim(ClaimTypes.Role, taikhoan.RoleId.ToString()),
                    new Claim(ClaimTypes.Email, taikhoan.Email)

                };
                ClaimsIdentity identity = new ClaimsIdentity(claims, "login");
                ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(principal);

                _cache.Remove(email);

                _notyfService.Success("Xác thực tài khoản thành công");
                return RedirectToAction("Index", "Home");
            }
            _notyfService.Error("Mã xác thực không đúng hoặc đã hết hạn.");
            ViewBag.Email = email;
            return View();
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("xac-thuc-tai-khoan/{email}/resend")]
        public async Task<IActionResult> ResendVerificationCode(string email)
        {
            var khachhang = _context.Users.FirstOrDefault(p => p.Email == email);
            string verificationCode = new Random().Next(100000, 999999).ToString();
            SaveVerificationCode(email, verificationCode);

            //Gửi email xác nhận
            string subject = "Xác nhận tài khoản đăng ký";
            string body = $"<p>Xin chào {khachhang.FullName},</p>" +
                          $"<p>Bạn đã đăng ký tài khoản tại hệ thống của chúng tôi. Đây là mã xác nhận tài khoản của bạn:</p>" +
                          $"<p>Mã xác nhận: {verificationCode}</p>" +
                          $"<p>Trân trọng,<br>Đội ngũ hỗ trợ</p>";

            EmailService emailService = new EmailService(_configuration);
            await emailService.SendEmailAsync(khachhang.Email, subject, body);
            return Json(new { success = true, message = "Mã xác thực mới đã được gửi! Vui lòng kiểm tra email." });
        }

        [AllowAnonymous]
        [Route("/dang-nhap", Name = "DangNhap")]
        public IActionResult Login(string returnUrl = null)
        {
            var taikhoanID = HttpContext.Session.GetString("UserId");
            if (taikhoanID != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/dang-nhap", Name = "DangNhap")]
        public async Task<IActionResult> Login(LoginViewModel customer, string returnUrl = null)
        {
            try
            {
                bool isEmail = Utilities.IsValidEmail(customer.Email);
                if (!isEmail) return View(customer);

                var khachhang = _context.Users.AsNoTracking().FirstOrDefault(x => x.Email.Trim() == customer.Email);

                if (khachhang == null)
                {
                    _notyfService.Error("Thông tin đăng nhập chưa chính xác");
                    return RedirectToAction("Login");
                }

                string pass = (customer.Password + khachhang.Salt.Trim()).ToMD5();

                if (khachhang.Password != pass)
                {
                    _notyfService.Error("Thông tin đăng nhập chưa chính xác");
                    return RedirectToAction("Login");
                }

                if (!khachhang.IsVerified)
                {
                    _notyfService.Warning("Tài khoản của bạn chưa được xác minh. Vui lòng kiểm tra email để xác nhận.");
                    return RedirectToAction("VerifyEmail", new { email = customer.Email });
                }

                HttpContext.Session.SetString("UserId", khachhang.UserId.ToString());

                var taikhoanID = HttpContext.Session.GetString("UserId");

                var claims = new List<Claim>()
                    {
                        new Claim(ClaimTypes.Name, khachhang.FullName),
                        new Claim("UserId", khachhang.UserId.ToString()),
                        new Claim(ClaimTypes.Role, khachhang.RoleId.ToString()),
                        new Claim(ClaimTypes.Email, khachhang.Email),
                    };
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "login");
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);
                _notyfService.Success("Đăng nhập thành công");

                if (khachhang.RoleId == 1)
                {
                    return RedirectToAction("Index", "Home", new { Area = "Admin" });
                }
                else
                {
                    returnUrl = HttpContext.Session.GetString("returnUrl");
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        // Xóa ReturnUrl khỏi session sau khi sử dụng
                        HttpContext.Session.Remove("returnUrl");
                        return Redirect(returnUrl);
                    }
                    if (Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("Index", "Home");
                }

            }
            catch
            {
                _notyfService.Error("Đăng nhập không thành công");
                return RedirectToAction("DangKyTaiKhoan", "Accounts");
            }
        }

        [HttpGet]
        [Route("/dang-xuat", Name = "Logout")]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync();
            HttpContext.Session.Remove("UserId");
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [Route("/quen-mat-khau", Name = "ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [Route("/quen-mat-khau", Name = "ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            return RedirectToAction("Login");
        }
    }
}
