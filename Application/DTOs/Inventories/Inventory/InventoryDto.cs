namespace Application.DTOs.Inventories.Inventory
{
    public class InventoryDto
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
    }
}