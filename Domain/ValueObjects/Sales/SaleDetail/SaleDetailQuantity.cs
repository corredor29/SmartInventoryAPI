using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Sales.SaleDetail
{
    public record SaleDetailQuantity
    {
        public int Value { get; }
        public SaleDetailQuantity(int value)
        {
            if (value <= 0)
                throw new ArgumentException("Sale detail quantity must be greater than 0.");
            Value = value;
        }
    }
}