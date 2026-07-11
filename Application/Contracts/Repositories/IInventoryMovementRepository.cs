using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Inventories;

namespace Application.Contracts.Repositories
{
    public interface IInventoryMovementRepository : IRepository<InventoryMovement>
    {
        Task<IReadOnlyList<InventoryMovement>> GetByInventoryIdAsync(int inventoryId);
    }
}
