using System.Collections.Generic;
using System.Collections.Generic;
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

        public override async Task<IReadOnlyList<Inventory>> GetAllAsync()
        {
            return await DbSet
                .Include(i => i.Product)
                .ToListAsync();
        }

        public override async Task<Inventory?> GetByIdAsync(int id)
        {
            return await DbSet
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Inventory?> GetByProductIdAsync(int productId)
        {
            return await DbSet
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.ProductId == productId);
        }
    }
}
