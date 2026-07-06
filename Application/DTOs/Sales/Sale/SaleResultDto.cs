namespace Application.DTOs.Sales.Sale
{
    public class SaleResultDto
    {
        public bool Success { get; set; }
        public int? SaleId { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal? Total { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}