using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.ValueObjects.Products.ProductStatus;

namespace Domain.Entities.Products
{
    public class ProductStatus 
    {
        public ProductStatusName Name { get; private set; } = null!;

        public ICollection<Product> Products { get; private set; } = new List<Product>();

        private ProductStatus() { }

        public ProductStatus(ProductStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void Update(ProductStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}