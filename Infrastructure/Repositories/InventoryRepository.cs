using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Inventories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class InventoryRepository : Repository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(AppDbContext context) : base(context) { }

        public async Task<Inventory?> GetByProductIdAsync(int productId)
        {
            return await DbSet.FirstOrDefaultAsync(i => i.ProductId == productId);
        }
    }
}