using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Inventories.Inventory;

namespace Application.Contracts.Services.Inventories
{
    public interface IInventoryService
    {
        Task<IReadOnlyList<InventoryDto>> GetAllAsync();
        Task<InventoryDto?> GetByIdAsync(int id);
        Task<InventoryDto?> GetByProductIdAsync(int productId);
        Task<InventoryDto?> AdjustStockAsync(int productId, UpdateInventoryRequest request);
    }
}