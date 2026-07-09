using System;
using System.Collections.Generic;

namespace Application.DTOs.Dashboard
{
    public class DashboardMetricsDto
    {
        public int TotalProducts { get; set; }
        public decimal DailySalesTotal { get; set; }
        public int LowStockCount { get; set; }
        public int ChatbotSalesCount { get; set; }
        public List<WeeklySalesPointDto> WeeklySales { get; set; } = new();
        public List<CategorySalesDto> CategorySales { get; set; } = new();
        public List<RecentInvoiceDto> RecentInvoices { get; set; } = new();
    }

    public class WeeklySalesPointDto
    {
        public string Day { get; set; } = string.Empty;
        public decimal Manual { get; set; }
        public decimal Chatbot { get; set; }
    }

    public class CategorySalesDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class RecentInvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public decimal Total { get; set; }
    }
}