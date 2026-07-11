using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Inventories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class InventoryMovementRepository : Repository<InventoryMovement>, IInventoryMovementRepository
    {
        public InventoryMovementRepository(AppDbContext context) : base(context) { }

        public override async Task<IReadOnlyList<InventoryMovement>> GetAllAsync()
        {
            return await DbSet
                .Include(m => m.MovementType)
                .Include(m => m.Inventory)
                    .ThenInclude(i => i!.Product)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public override async Task<InventoryMovement?> GetByIdAsync(int id)
        {
            return await DbSet
                .Include(m => m.MovementType)
                .Include(m => m.Inventory)
                    .ThenInclude(i => i!.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IReadOnlyList<InventoryMovement>> GetByInventoryIdAsync(int inventoryId)
        {
            return await DbSet
                .Include(m => m.MovementType)
                .Include(m => m.Inventory)
                    .ThenInclude(i => i!.Product)
                .Where(m => m.InventoryId == inventoryId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}
