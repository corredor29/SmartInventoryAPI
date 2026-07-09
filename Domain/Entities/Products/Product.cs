using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Sales;
using Domain.ValueObject.Products.Product;
using Domain.Entities.Inventories;
namespace Domain.Entities.Products
{
    public sealed class Product : BaseEntity
    {
        public int                CategoryId      { get; private set; }
        public int                ProductStatusId { get; private set; }
        public ProductName        Name            { get; private set; } = null!;
        public ProductDescription? Description    { get; private set; }
        public ProductPrice       Price           { get; private set; } = null!;
        public Pgvector.Vector?   Embedding       { get; private set; }
        public string?            ImageUrl        { get; private set; }

        public Category       Category      { get; private set; } = null!;
        public ProductStatus  ProductStatus { get; private set; } = null!;
        public Inventory?     Inventory     { get; private set; }
        public ICollection<SaleDetail> SaleDetails { get; private set; } = new List<SaleDetail>();

        private Product() { }

        public Product(int categoryId, int productStatusId, ProductName name, ProductPrice price, ProductDescription? description = null, string? imageUrl = null)
        {
            CategoryId      = categoryId      > 0 ? categoryId      : throw new ArgumentException("CategoryId must be greater than 0.");
            ProductStatusId = productStatusId > 0 ? productStatusId : throw new ArgumentException("ProductStatusId must be greater than 0.");
            Name            = name  ?? throw new ArgumentNullException(nameof(name));
            Price           = price ?? throw new ArgumentNullException(nameof(price));
            Description     = description;
            ImageUrl        = imageUrl;
        }

        public void Update(ProductName name, ProductPrice price, ProductDescription? description, int categoryId, string? imageUrl)
        {
            Name        = name  ?? throw new ArgumentNullException(nameof(name));
            Price       = price ?? throw new ArgumentNullException(nameof(price));
            Description = description;
            CategoryId  = categoryId > 0 ? categoryId : throw new ArgumentException("CategoryId must be greater than 0.");
            ImageUrl    = imageUrl;
        }

        public void ChangeStatus(int productStatusId)
        {
            ProductStatusId = productStatusId > 0 ? productStatusId : throw new ArgumentException("ProductStatusId must be greater than 0.");
        }

        public void SetEmbedding(Pgvector.Vector embedding)
        {
            Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        }

        public void SetImageUrl(string? imageUrl)
        {
            ImageUrl = imageUrl;
        }
    }
}