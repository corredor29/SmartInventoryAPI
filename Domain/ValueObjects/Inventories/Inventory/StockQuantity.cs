using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Inventories.Inventory
{
    public record StockQuantity
    {
        public int Value { get; }
        public StockQuantity(int value)
        {
            if (value < 0)
                throw new ArgumentException("Stock quantity cannot be negative.");
            Value = value;
        }

        public static StockQuantity Zero => new(0);

        public StockQuantity Increase(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount to increase must be greater than 0.");
            return new StockQuantity(Value + amount);
        }

        public StockQuantity Decrease(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount to decrease must be greater than 0.");
            if (amount > Value)
                throw new InvalidOperationException("Insufficient stock to decrease the requested amount.");
            return new StockQuantity(Value - amount);
        }

        public bool HasEnoughFor(int requestedAmount) => Value >= requestedAmount;
    }
}