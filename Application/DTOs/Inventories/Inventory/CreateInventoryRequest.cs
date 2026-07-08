namespace Application.DTOs.Inventories.Inventory
{
    public class CreateInventoryRequest
    {
        public int ProductId { get; set; }
        public int InitialStock { get; set; } = 0;
    }
}