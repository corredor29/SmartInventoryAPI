using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Inventories.InventoryMovement
{
    public record MovementQuantity
    {
        public int Value { get; }
        public MovementQuantity(int value)
        {
            if (value <= 0)
                throw new ArgumentException("Movement quantity must be greater than 0.");
            Value = value;
        }
    }
}