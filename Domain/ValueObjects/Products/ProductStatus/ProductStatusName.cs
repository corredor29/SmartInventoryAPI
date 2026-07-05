using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObjects.Products.ProductStatus
{
    public sealed record ProductStatusName
    {
        public string Value{ get; private set;} = default!;
        private ProductStatusName(){}
        private ProductStatusName(string value)
        {
            Value = value;
        }
        public static ProductStatusName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Product status name cannot be empty.");
            if (value.Length > 150)
                throw new ArgumentException("Product status name cannot exceed 150 characters.");
            return new ProductStatusName(value.Trim());
        }
        public override string ToString() => Value;
        
    }
}