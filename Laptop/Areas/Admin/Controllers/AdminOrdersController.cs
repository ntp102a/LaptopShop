using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LaptopShop.Models;
using AspNetCoreHero.ToastNotification.Abstractions;
using PagedList.Core;
using LaptopShop.ModelViews;
using LaptopShop.Helpper;

namespace LaptopShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly laptopWebContext _context;
        public INotyfService _notyfService { get; }

        public AdminOrdersController(laptopWebContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        // GET: Admin/AdminOrders
        public IActionResult Index(int? page)
        {
            var pageNumber = page == null || page <= 0 ? 1 : page.Value;
            var pageSize = 20;
            var IsOrders = _context.Orders.Include(o => o.User).Include(o => o.Status)
                .AsNoTracking()
                .OrderByDescending(x => x.OrderDate);
            PagedList<Order> models = new PagedList<Order>(IsOrders, pageNumber, pageSize);
            ViewBag.CurrentPage = pageNumber;
            return View(models);
        }

        // GET: Admin/AdminOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Status)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            var Chitietdonhang = _context.OrderDetails
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.OrderId == order.OrderId)
                .OrderBy(x => x.OrderDetailId)
                .ToList();
            ViewBag.ChiTiet = Chitietdonhang;

            var fullAddress = $"{order.Address}";
            ViewBag.fullAddress = fullAddress;
            return View(order);
        }

        public async Task<IActionResult> ChangeStatus(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Status)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            //.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["TrangThai"] = new SelectList(_context.TransactStatuses, "StatusId", "Status", order.StatusId);
            return PartialView("ChangeStatus", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var donhang = await _context.Orders
                        .AsNoTracking()
                        .Include(x => x.User)
                        .FirstOrDefaultAsync(x => x.OrderId == id);
                    if (donhang != null)
                    {
                        donhang.StatusId = order.StatusId;
                        donhang.Phone = donhang.User.Phone;
                    }
                    _context.Orders.Update(donhang);
                    await _context.SaveChangesAsync();
                    _notyfService.Success("Cập nhật thành công");

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
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
            ViewData["TrangThai"] = new SelectList(_context.TransactStatuses, "StatusId", "Status", order.StatusId);
            return PartialView("ChangStatus", order);
        }

        // GET: Admin/AdminOrders/Create
        public IActionResult Create()
        {
            ViewData["StatusId"] = new SelectList(_context.TransactStatuses, "StatusId", "StatusId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Admin/AdminOrders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,Address,OrderDate,Phone,Total,UserId,StatusId")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                _notyfService.Success("Thêm thành công");
                return RedirectToAction(nameof(Index));
            }
            ViewData["StatusId"] = new SelectList(_context.TransactStatuses, "StatusId", "StatusId", order.StatusId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", order.UserId);
            return View(order);
        }

        // GET: Admin/AdminOrders/Edit/5
        public async Task<IActionResult> Edit(int? id, OrderViewModel model)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.Include(x => x.User).Include(x => x.Status).FirstOrDefaultAsync(x => x.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            else
            {
                model.OrderId = order.OrderId;
                model.RecipientName = order.RecipientName;
                model.Address = order.Address;
                model.Phone = order.Phone;
                model.OrderDate = order.OrderDate;
                model.Total = order.Total;
                model.Note = order.Note;
                model.UserId = order.UserId;
                model.StatusId = order.StatusId;
            }
            ViewData["StatusId"] = new SelectList(_context.TransactStatuses, "StatusId", "Status", model.StatusId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", model.UserId);
            return View(model);
        }

        // POST: Admin/AdminOrders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderViewModel model)
        {
            if (id != model.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var order = _context.Orders.SingleOrDefault(x => x.OrderId == model.OrderId);
                    if (order != null)
                    {
                        order.RecipientName = model.RecipientName;
                        order.Address = model.Address;
                        order.Phone = model.Phone;
                        order.OrderDate = model.OrderDate;
                        order.Total = model.Total;
                        order.Note = model.Note;
                        order.UserId = model.UserId;
                        order.StatusId = model.StatusId;
                        _context.Orders.Update(order);
                        await _context.SaveChangesAsync();
                        _notyfService.Success("Sửa thành công");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(model.OrderId))
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

        // GET: Admin/AdminOrders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Status)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Admin/AdminOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Orders == null)
            {
                return Problem("Entity set 'laptopWebContext.Orders'  is null.");
            }
            var order = _context.Orders.FirstOrDefault(x => x.OrderId == id);
            if (order != null)
            {
                var orderDetail = _context.OrderDetails.Where(x => x.OrderId == id).ToList();
                if (orderDetail != null)
                {
                    foreach(var item in orderDetail)
                    {
                        _context.OrderDetails.Remove(item);
                    }    
                    
                    _context.SaveChanges();
                }
                _context.Orders.Remove(order);
            }


            await _context.SaveChangesAsync();
            _notyfService.Success("Xoá thành công");
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return (_context.Orders?.Any(e => e.OrderId == id)).GetValueOrDefault();
        }
    }
}
