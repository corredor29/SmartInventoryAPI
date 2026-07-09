namespace Application.DTOs.Products.Product
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public string? ImageUrl { get; set; }
    }
}