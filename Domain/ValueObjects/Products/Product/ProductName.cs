using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Products.Product
{
    public record ProductName
    {
        public string Value { get; }
        public ProductName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Product name cannot be empty.");
            if (value.Length > 150)
                throw new ArgumentException("Product name cannot exceed 150 characters.");
            Value = value.Trim();
        }
    }
}