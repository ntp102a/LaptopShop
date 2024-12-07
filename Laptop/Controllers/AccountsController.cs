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

namespace LaptopShop.Controllers
{
    [Authorize]
    public class AccountsController : Controller
    {
        private readonly laptopWebContext _context;
        private readonly IConfiguration _configuration;

        public INotyfService _notyfService { get; }
        public AccountsController(laptopWebContext context, INotyfService notyfService, IConfiguration configuration)
        {
            _context = context;
            _notyfService = notyfService;
            _configuration = configuration;
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
            var taikhoanID = HttpContext.Session.GetString("UserId");
            if (taikhoanID != null)
            {
                var khachhang = _context.Users
                    .AsNoTracking()
                    .SingleOrDefault(x => x.UserId == taikhoanID);
                if (khachhang != null)
                {
                    var lsDonhang = _context.Orders
                        .Include(x => x.Status)
                        .AsNoTracking()
                        .Where(x => x.UserId == khachhang.UserId)
                        .OrderByDescending(x => x.OrderDate).ToList();
                    ViewBag.Donhang = lsDonhang;
                    return View(khachhang);
                }
            }
            return RedirectToAction("Login");
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

                    User khachhang = new User
                    {
                        UserId = Guid.NewGuid().ToString(),
                        FullName = taikhoan.Fullname,
                        Phone = taikhoan.Phone.Trim().ToLower(),
                        Email = taikhoan.Email.Trim().ToLower(),
                        Password = (taikhoan.Password + salt.Trim()).ToMD5(),
                        Salt = salt,
                        RoleId = 2,
                        isVerified = false,
                    };
                    try
                    {
                        _context.Add(khachhang);
                        await _context.SaveChangesAsync();

                        var UrlVerifiAccount = Url.Action("VerifyAccount", "Accounts", new { userId = khachhang.UserId }, Request.Scheme);

                        //Luu session UserId
                        HttpContext.Session.SetString("UserId", khachhang.UserId.ToString());
                        var taikhoanID = HttpContext.Session.GetString("UserId");
						//Gửi email xác nhận
						string subject = "Xác nhận tài khoản đăng ký";
						string body = $"<p>Xin chào {khachhang.FullName},</p>" +
									  $"<p>Bạn đã đăng ký tài khoản tại hệ thống của chúng tôi. Vui lòng nhấn vào liên kết bên dưới để kích hoạt tài khoản:</p>" +
									  $"<p><a href='{UrlVerifiAccount}'>Xác nhận tài khoản</a></p>" +
									  $"<p>Trân trọng,<br>Đội ngũ hỗ trợ</p>";

						EmailService emailService = new EmailService(_configuration);
                        await emailService.SendEmailAsync(khachhang.Email, subject, body);
                        _notyfService.Success("Đăng ký thành công. Vui lòng kiểm tra email.");

                        return RedirectToAction("Login", "Accounts");
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

		[HttpGet("VerifyAccount")]
		public async Task<IActionResult> VerifyAccount(string userId)
		{
			// Tìm người dùng theo userId
			var user = _context.Users.FirstOrDefault(x => x.UserId == userId);
			if (user == null)
			{
				return NotFound("Người dùng không tồn tại.");
			}

			// Kiểm tra xem tài khoản đã được xác nhận chưa
			if (user.isVerified)
			{
				_notyfService.Success("Tài khoản đã được xác nhận trước đó.");
				return RedirectToAction("Login", "Accounts");
			}

			// Xác nhận tài khoản
			user.isVerified = true;
			_context.SaveChanges();

			// Tạo thông tin đăng nhập và đăng nhập người dùng
			var claims = new List<Claim>
	        {
		        new Claim(ClaimTypes.Name, user.FullName),
		        new Claim("UserId", user.UserId),
		        new Claim(ClaimTypes.Email, user.Email),
		        new Claim(ClaimTypes.Role, user.RoleId.ToString()),
	        };
			ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "login");
			ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
			await HttpContext.SignInAsync(claimsPrincipal);
			_notyfService.Success("Tài khoản đã được xác nhận và đăng nhập thành công.");
			// Redirect đến trang chủ hoặc trang Dashboard sau khi đăng nhập
			return RedirectToAction("Index", "Home");
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
                if(!khachhang.isVerified)
                {
					_notyfService.Warning("Tài khoản của bạn chưa được xác minh. Vui lòng kiểm tra email để xác nhận.");
					return RedirectToAction("Login");
				}

                string pass = (customer.Password + khachhang.Salt.Trim()).ToMD5();

                if (khachhang.Password != pass)
                {
                    _notyfService.Error("Thông tin đăng nhập chưa chính xác");
                    return RedirectToAction("Login");
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

        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                var taikhoanID = HttpContext.Session.GetString("UserId");
                if (taikhoanID == null)
                {
                    return RedirectToAction("Login", "Accounts");
                }
                var taikhoan = _context.Users.Find(Convert.ToInt32(taikhoanID));
                if (taikhoan == null) return RedirectToAction("Login", "Accounts");
                if (model.PasswordNew.Length < 5)
                {
                    _notyfService.Error("Vui lòng nhập tối tiểu 5 ký tự");
                    return RedirectToAction("Dashboard", "Accounts");
                }
                else
                {
                    if (model.PasswordNew != model.ConfirmPasswordNew)
                    {
                        _notyfService.Error("Mật khẩu mới không trùng khớp");
                        return RedirectToAction("Dashboard", "Accounts");
                    }
                    var pass = (model.PasswordNow.Trim() + taikhoan.Salt.Trim()).ToMD5();
                    if (pass == taikhoan.Password)
                    {
                        string passNew = (model.PasswordNew.Trim() + taikhoan.Salt.Trim()).ToMD5();
                        taikhoan.Password = passNew;
                        _context.Update(taikhoan);
                        _context.SaveChanges();
                        _notyfService.Success("Thay đổi mật khẩu thành công");
                        return RedirectToAction("Dashboard", "Accounts");
                    }
                    else
                    {
                        _notyfService.Error("Sai mật khẩu");
                        return RedirectToAction("Dashboard", "Accounts");
                    }
                }
            }
            catch
            {
                _notyfService.Error("Thay đổi mật khẩu không thành công");
                return RedirectToAction("Dashboard", "Accounts");
            }
        }
        [Authorize]
        [HttpPost]
        public IActionResult ChangeInfo(ChangeInfoViewModel model)
        {
            try
            {
                var accountID = User.Identity.GetAccountID();

                if (!string.IsNullOrEmpty(accountID))
                {
                    var existingUser = _context.Users.SingleOrDefault(x => x.Phone.ToLower() == model.Phone.ToLower());
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Phone", "Số điện thoại đã tồn tại");
                        _notyfService.Success("Số điện thoại đã tồn tại");
                        return RedirectToAction("Dashboard", "Accounts");
                    }
                    var user = _context.Users.SingleOrDefault(x => x.UserId == accountID);
                    if (model != null)
                    {
                        user.FullName = model.FullName;
                        user.Address = model.Address;
                        user.Phone = model.Phone;
                        _context.Update(user);
                        _context.SaveChanges();
                        _notyfService.Success("Thay đổi thành công");
                        return RedirectToAction("Dashboard", "Accounts");
                    }
                }
                return RedirectToAction("Dashboard", "Accounts");
            }
            catch
            {
                _notyfService.Error("Thay đổi không thành công");
                return RedirectToAction("Dashboard", "Accounts");
            }
        }
    }
}
