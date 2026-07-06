using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Products;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Product>> SearchAsync(string query)
        {
            var lowered = query.ToLower();

            return await DbSet
                .Include(p => p.Category)
                .Include(p => p.ProductStatus)
                .Include(p => p.Inventory)
                .Where(p =>
                    EF.Functions.ILike(p.Name.Value, $"%{lowered}%") ||
                    (p.Description != null && EF.Functions.ILike(p.Description.Value, $"%{lowered}%")))
                .ToListAsync();
        }

        public async Task<Product?> GetByIdWithInventoryAsync(int productId)
        {
            return await DbSet
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }
    }
}