namespace Application.DTOs.Inventories.Inventory
{
    public class UpdateInventoryRequest
    {
        public int QuantityChange { get; set; }
        public string? Reason { get; set; }
    }
}