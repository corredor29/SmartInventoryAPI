using System;
using System.Collections.Generic;
using Application.DTOs.Sales.SaleDetail;

namespace Application.DTOs.Sales.Sale
{
    public class SaleDto
    {
        public int SaleId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string OriginName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal Total { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? DeliveryAddress { get; set; }
        public decimal? DeliveryLat { get; set; }
        public decimal? DeliveryLng { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactDocument { get; set; }
        public List<SaleDetailDto> Details { get; set; } = new();
    }
}
