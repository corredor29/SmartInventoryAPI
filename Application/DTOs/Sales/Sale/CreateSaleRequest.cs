using System.Collections.Generic;

namespace Application.DTOs.Sales.Sale
{
    public class CreateSaleRequest
    {
        public int? CustomerId { get; set; }
        public string? SessionId { get; set; }
        public List<SaleItemRequest> Items { get; set; } = new();
        public string Origin { get; set; } = "Manual";
    }

    public class SaleItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}