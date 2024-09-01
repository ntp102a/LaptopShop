using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LaptopShop.Models;
using PagedList.Core;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using AspNetCoreHero.ToastNotification.Abstractions;
using LaptopShop.Helpper;
using LaptopShop.Extension;
using LaptopShop.ModelViews;

namespace LaptopShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class AdminUsersController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }

        public AdminUsersController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        // GET: Admin/AdminUsers
        public async Task<IActionResult> Index(int? page)
        {
            var pageNumber = page == null || page <= 0 ? 1 : page.Value;
            var pageSize = 20;
            var IsCustomers = _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .OrderByDescending(x => x.UserId);
            PagedList<User> models = new PagedList<User>(IsCustomers, pageNumber, pageSize);
            ViewBag.CurrentPage = pageNumber;
            return View(models);
        }

        // GET: Admin/AdminUsers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Admin/AdminUsers/Create
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            return View();
        }

        // POST: Admin/AdminUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            user.UserId = Guid.NewGuid().ToString();
            string salt = Utilities.GetRandomKey();
            user.Salt = salt;

            //tạo ngẫu nhiên pass
            user.Password = (user.Password + salt.Trim()).ToMD5();

            _context.Add(user);
            await _context.SaveChangesAsync();
            _notyfService.Success("Tạo mới thành công");
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/AdminUsers/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id) || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                Address = user.Address,
                Password = user.Password,
                Salt = user.Salt,
                RoleId = user.RoleId
            };

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", model.RoleId);
            return View(model);
        }

        // POST: Admin/AdminUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = _context.Users.SingleOrDefault(x => x.UserId == model.UserId);
                    if (user != null)
                    {
                        user.FullName = model.FullName;
                        user.Phone = model.Phone;
                        user.Email = model.Email;
                        user.Address = model.Address;
                        user.RoleId = model.RoleId;
                        if (user.Password != (model.Password + user.Salt.Trim()).ToMD5())
                        {
                            string salt = Utilities.GetRandomKey();
                            user.Salt = salt;
                            user.Password = (model.Password + salt.Trim()).ToMD5();
                        }
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                        _notyfService.Success("Sửa thành công");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(model.UserId))
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
            return View(model);
        }

        // GET: Admin/AdminUsers/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/AdminUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'laptopWebContext.Users'  is null.");
            }
            
            var cart = _context.Carts.FirstOrDefault(x => x.UserId == id);
            if (cart != null)
            {
                _context.Carts.Remove(cart);
                _context.SaveChanges();
            }

            var order = _context.Orders.Where(x => x.UserId == id).ToList();
            if (order != null)
            {
                foreach (var item in order)
                {
                    var orderDetail = _context.OrderDetails.Where(x => x.OrderId == item.OrderId).ToList();
                    if(orderDetail != null)
                    {
                        foreach (var items in orderDetail)
                        {
                            _context.OrderDetails.Remove(items);
                        }
                        _context.SaveChanges();
                    }
                    _context.Orders.Remove(item);
                    _context.SaveChanges();
                }
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            _notyfService.Success("Xoá thành công");
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(string id)
        {
            return (_context.Users?.Any(e => e.UserId == id)).GetValueOrDefault();
        }
    }
}
