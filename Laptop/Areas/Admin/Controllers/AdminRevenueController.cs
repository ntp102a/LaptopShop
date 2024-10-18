using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LaptopShop.Models;
using OfficeOpenXml;
using System.IO;
using OfficeOpenXml.Drawing.Chart;

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

        public IActionResult ExportDataToExcel(int year)
        {
            try
            {
                var monthlyRevenue = new Dictionary<int, decimal>();
                for (int month = 1; month <= 12; month++)
                {
                    monthlyRevenue[month] = 0; // Default revenue is 0
                }
                var monthlyDatalist = _context.Orders
                    .Where(o => o.StatusId == 2 || o.StatusId == 5) // Filter by status
                    .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Year == year)
                    .GroupBy(o => o.OrderDate.Value.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Total = g.Sum(o => o.Total) // Total revenue for the month
                    })
                    .ToList();

                // Populate the dictionary with actual revenue data
                foreach (var data in monthlyDatalist)
                {
                    monthlyRevenue[data.Month] = (decimal)data.Total; // Assign the revenue to the respective month
                }

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Revenue Report");

                    // Header
                    worksheet.Cells[1, 1].Value = "Tháng";
                    worksheet.Cells[1, 2].Value = "Doanh Thu";

                    // Data
                    int row = 2;
                    for (int month = 1; month <= 12; month++)
                    {
                        worksheet.Cells[row, 1].Value = month; // Month
                        worksheet.Cells[row, 2].Value = monthlyRevenue[month]; // Revenue as a decimal

                        worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";

                        // Format Header
                        worksheet.Cells[1, 1, 1, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center; // Center alignment
                        worksheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;
                        worksheet.Cells[1, 1, 1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, 1, 1, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                        worksheet.Column(1).Width = 15;
                        worksheet.Column(2).Width = 30;

                        // Kẻ viền
                        worksheet.Cells[row, 1, row, 2].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black);

                        // Format data rows
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                        // Fill colors
                        worksheet.Cells[row, 1, row, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        row++;
                    }

                    var chart = worksheet.Drawings.AddChart("RevenueChart", OfficeOpenXml.Drawing.Chart.eChartType.BarClustered); 
                    chart.Title.Text = "Doanh Thu Theo Tháng";
                    chart.SetPosition(row + 2, 0, 0, 0); 
                    chart.SetSize(1200, 600); 

                    // Specify the data for the chart
                    var series = chart.Series.Add(worksheet.Cells[2, 2, 13, 2], worksheet.Cells[2, 1, 13, 1]); 
                                                                                                               

                    // Export to memory stream
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var fileName = $"RevenueReport_{year}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"; 
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExportDataToExcel: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }











    }
}
