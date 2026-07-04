using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Products.Product
{
    public record ProductPrice
    {
        public decimal Value { get; }
        public ProductPrice(decimal value)
        {
            if (value < 0)
                throw new ArgumentException("Product price cannot be negative.");
            Value = Math.Round(value, 2);
        }

        public static ProductPrice operator *(ProductPrice price, int quantity) => new(price.Value * quantity);
    }
}