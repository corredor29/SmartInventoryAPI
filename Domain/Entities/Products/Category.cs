using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObjects.Products.Category;

namespace Domain.Entities.Products
{

    public sealed class Category : BaseEntity
    {
            public CategoryName Name { get; private set; } = null!;

            public ICollection<Product> Products { get; private set; } = new List<Product>();

            private Category() { }

            public Category(CategoryName name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public void Update(CategoryName name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }
    }
    
}