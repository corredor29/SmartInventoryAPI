using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Inventories.InventoryMovement
{
    public record MovementReason
    {
        public string Value { get; }
        public MovementReason(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > 255)
                throw new ArgumentException("Movement reason cannot exceed 255 characters.");
            Value = value.Trim();
        }
    }
}