using System.Threading.Tasks;
using Domain.Entities.Inventories;

namespace Application.Contracts.Repositories
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task<Inventory?> GetByProductIdAsync(int productId);
    }
}