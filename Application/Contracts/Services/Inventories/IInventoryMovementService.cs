using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Inventories.InventoryMovement;

namespace Application.Contracts.Services.Inventories
{
    public interface IInventoryMovementService
    {
        Task<IReadOnlyList<InventoryMovementDto>> GetAllAsync();
        Task<InventoryMovementDto?> GetByIdAsync(int id);
        Task<IReadOnlyList<InventoryMovementDto>> GetByInventoryIdAsync(int inventoryId);
    }
}