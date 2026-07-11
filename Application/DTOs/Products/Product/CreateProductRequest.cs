namespace Application.DTOs.Products.Product
{
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public int ProductStatusId { get; set; }
        public string? ImageUrl { get; set; }
        /// <summary>Stock inicial al crear el producto (por defecto 0).</summary>
        public int InitialStock { get; set; }
    }
}