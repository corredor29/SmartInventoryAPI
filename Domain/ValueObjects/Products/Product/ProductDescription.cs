using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Products.Product
{
    public record ProductDescription
    {
        public string Value { get; }
        public ProductDescription(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > 1000)
                throw new ArgumentException("Product description cannot exceed 1000 characters.");
            Value = value.Trim();
        }
    }
}