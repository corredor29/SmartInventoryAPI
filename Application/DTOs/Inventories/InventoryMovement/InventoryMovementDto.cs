using System;

namespace Application.DTOs.Inventories.InventoryMovement
{
    public class InventoryMovementDto
    {
        public int MovementId { get; set; }
        public int InventoryId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string MovementTypeName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}