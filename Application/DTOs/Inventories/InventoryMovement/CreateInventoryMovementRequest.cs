namespace Application.DTOs.Inventories.InventoryMovement
{
    public class CreateInventoryMovementRequest
    {
        public int InventoryId { get; set; }
        public int MovementTypeId { get; set; }
        public int Quantity { get; set; }
        public string? Reason { get; set; }
    }
}