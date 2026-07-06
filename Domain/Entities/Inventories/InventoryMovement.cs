using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObject.Inventories.InventoryMovement;
using Domain.Entities.Products;
namespace Domain.Entities.Inventories
{
    public sealed class InventoryMovement : BaseEntity
    {
        public int              InventoryId    { get; private set; }
        public int              MovementTypeId { get; private set; }
        public MovementQuantity Quantity       { get; private set; } = null!;
        public MovementReason?  Reason         { get; private set; }

        public Inventory     Inventory     { get; private set; } = null!;
        public MovementType  MovementType  { get; private set; } = null!;

        private InventoryMovement() { }

        public InventoryMovement(int inventoryId, int movementTypeId, MovementQuantity quantity, MovementReason? reason = null)
        {
            InventoryId    = inventoryId    > 0 ? inventoryId    : throw new ArgumentException("InventoryId must be greater than 0.");
            MovementTypeId = movementTypeId > 0 ? movementTypeId : throw new ArgumentException("MovementTypeId must be greater than 0.");
            Quantity       = quantity ?? throw new ArgumentNullException(nameof(quantity));
            Reason         = reason;
        }

        public static InventoryMovement CreateExit(int inventoryId, int movementTypeId, int quantity, string? reason = null)
            => new(inventoryId, movementTypeId, new MovementQuantity(quantity), reason is null ? null : new MovementReason(reason));
    }
}