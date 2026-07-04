using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Sales.Sale
{
    public record SaleDate
    {
        public DateTime Value { get; }
        public SaleDate(DateTime value)
        {
            if (value > DateTime.UtcNow.AddMinutes(5))
                throw new ArgumentException("Sale date cannot be in the future.");
            Value = value;
        }

        public static SaleDate Now() => new(DateTime.UtcNow);
    }
}