using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LaptopShop.Models;

namespace LaptopShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminRevenueController : Controller
    {
        private readonly laptopWebContext _context;

        public AdminRevenueController(laptopWebContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminRevenue
        public IActionResult Index()
        {
            var totalMoney = _context.Orders
                .Where(o => o.StatusId == 2 || o.StatusId == 5 || o.StatusId == 4)
                .Sum(o => o.Total);

            var totalOrders = _context.Orders.Count();

            var totalUser = _context.Orders
                .GroupBy(o => o.UserId)
                .Select(g => g.Key)
                .Count();

            #region Doanh thu theo tháng
            var monthlyData = _context.Orders
                .OrderBy(o => o.OrderDate)
                .GroupBy(o => new { Month = o.OrderDate.Value.Month, Year = o.OrderDate.Value.Year })
                .Select(g => new { MonthYear = $"{g.Key.Month:00}/{g.Key.Year}", Total = g.Sum(o => o.Total) });
            ViewBag.MonthlyData = monthlyData;
            #endregion

            ViewBag.TotalSum = totalMoney;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalOrdersUser = totalUser;
            return View();
        }

        public IActionResult ExportData(int year, int month)
        {
            // Tạo mảng để lưu trữ doanh thu của từng ngày trong tháng
            int[] dailyDataArray = new int[DateTime.DaysInMonth(year, month)];

            var orderData = _context.Orders
                .Where(o => o.StatusId == 2 || o.StatusId == 5 || o.StatusId == 4)
                .Where(o => o.OrderDate.HasValue &&
                            o.OrderDate.Value.Year == year &&
                            o.OrderDate.Value.Month == month)
                .OrderBy(o => o.OrderDate)
                .ToList();

            int totalRevenueInMonth = 0; // Tổng giá trị của tất cả các ngày

            // Điền giá trị từ dữ liệu thực tế vào mảng
            foreach (var order in orderData)
            {
                int dayIndex = order.OrderDate.Value.Day - 1;
                dailyDataArray[dayIndex] += order.Total ?? 0;

                // Cập nhật tổng giá trị của tất cả các ngày
                totalRevenueInMonth += order.Total ?? 0;
            }

            // Tổng doanh thu của năm
            int totalRevenueInYear = _context.Orders
                            .Where(o => o.StatusId == 2 || o.StatusId == 5)
                            .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Year == year)
                            .Sum(o => o.Total) ?? 0;

            // Tạo danh sách đối tượng để trả về cho doanh thu hàng ngày
            var dailyDataList = new List<dynamic>();
            for (int i = 0; i < dailyDataArray.Length; i++)
            {
                dailyDataList.Add(new { Date = i + 1, Total = dailyDataArray[i] });
            }

            // Tạo danh sách đối tượng để trả về cho doanh thu hàng tháng trong năm
            var monthlyDataList = new List<dynamic>();
            for (int i = 1; i <= 12; i++)
            {
                var monthlyTotal = _context.Orders
                    .Where(o => o.StatusId == 2 || o.StatusId == 5)
                    .Where(o => o.OrderDate.HasValue &&
                                o.OrderDate.Value.Year == year &&
                                o.OrderDate.Value.Month == i)
                    .Sum(o => o.Total) ?? 0;

                monthlyDataList.Add(new { Month = i, Total = monthlyTotal });
            }

            // Format data to match the response structure
            var responseData = new
            {
                isEmpty = dailyDataList.Count == 0,
                data = dailyDataList,
                totalRevenueInMonth,
                totalRevenueInYear,
                monthlyData = monthlyDataList
            };

            return Json(responseData);
        }




    }
}
