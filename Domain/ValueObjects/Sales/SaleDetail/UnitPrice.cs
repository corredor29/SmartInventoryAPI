using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Sales.SaleDetail
{
    public record UnitPrice
    {
        public decimal Value { get; }
        public UnitPrice(decimal value)
        {
            if (value < 0)
                throw new ArgumentException("Unit price cannot be negative.");
            Value = Math.Round(value, 2);
        }
    }
}