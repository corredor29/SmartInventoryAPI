using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Dashboard;
using Application.DTOs.Dashboard;
using Domain.Entities.Sales;

namespace Application.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {

        private const int LowStockThreshold = 10;

        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardMetricsDto> GetMetricsAsync()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            var inventories = await _unitOfWork.Inventory.GetAllAsync();
            var sales = await _unitOfWork.Sales.GetAllWithDetailsAsync();

            var today = DateTime.UtcNow.Date;
            var todaySales = sales.Where(s => s.SaleDate.Value.Date == today).ToList();

            return new DashboardMetricsDto
            {
                TotalProducts = products.Count,
                DailySalesTotal = todaySales.Sum(s => s.GetTotal()),
                LowStockCount = inventories.Count(i => i.CurrentStock.Value < LowStockThreshold),
                ChatbotSalesCount = sales.Count(s => s.SaleOrigin?.Name.Value == "Chatbot"),
                WeeklySales = BuildWeeklySales(sales),
                CategorySales = BuildCategorySales(sales),
                RecentInvoices = await BuildRecentInvoicesAsync(),
            };
        }

        private static List<WeeklySalesPointDto> BuildWeeklySales(IReadOnlyList<Sale> sales)
        {
            var culture = CultureInfo.GetCultureInfo("es-ES");
            var today = DateTime.UtcNow.Date;
            var last7Days = Enumerable.Range(0, 7).Select(offset => today.AddDays(-6 + offset));

            return last7Days.Select(day =>
            {
                var daySales = sales.Where(s => s.SaleDate.Value.Date == day).ToList();

                return new WeeklySalesPointDto
                {
                    Day = culture.TextInfo.ToTitleCase(day.ToString("ddd", culture)).Replace(".", ""),
                    Manual = daySales.Where(s => s.SaleOrigin?.Name.Value == "Manual").Sum(s => s.GetTotal()),
                    Chatbot = daySales.Where(s => s.SaleOrigin?.Name.Value == "Chatbot").Sum(s => s.GetTotal()),
                };
            }).ToList();
        }

        private static List<CategorySalesDto> BuildCategorySales(IReadOnlyList<Sale> sales)
        {
            return sales
                .SelectMany(s => s.Details)
                .Where(d => d.Product?.Category != null)
                .GroupBy(d => d.Product!.Category.Name.Value)
                .Select(g => new CategorySalesDto
                {
                    CategoryName = g.Key,
                    Value = g.Sum(d => d.GetSubtotal()),
                })
                .OrderByDescending(c => c.Value)
                .Take(5)
                .ToList();
        }

        private async Task<List<RecentInvoiceDto>> BuildRecentInvoicesAsync()
        {
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var recent = invoices.OrderByDescending(i => i.IssueDate.Value).Take(5).ToList();

            var result = new List<RecentInvoiceDto>();

            foreach (var invoice in recent)
            {
                var sale = await _unitOfWork.Sales.GetByIdWithDetailsAsync(invoice.SaleId);
                if (sale is null) continue;

                result.Add(new RecentInvoiceDto
                {
                    InvoiceNumber = invoice.InvoiceNumber.Value,
                    CustomerName = sale.Customer?.Name.Value ?? string.Empty,
                    IssueDate = invoice.IssueDate.Value,
                    Total = sale.GetTotal(),
                });
            }

            return result;
        }
    }
}