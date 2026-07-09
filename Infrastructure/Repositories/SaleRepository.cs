using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Sales;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class SaleRepository : Repository<Sale>, ISaleRepository
    {
        public SaleRepository(AppDbContext context) : base(context) { }

        public async Task<Sale?> GetByIdWithDetailsAsync(int saleId)
        {
            return await DbSet
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .Include(s => s.Customer)
                .Include(s => s.SaleOrigin)
                .Include(s => s.SaleStatus)
                .FirstOrDefaultAsync(s => s.Id == saleId);
        }

        public async Task<IReadOnlyList<Sale>> GetAllWithDetailsAsync()
        {
            return await DbSet
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.Category)
                .Include(s => s.Customer)
                .Include(s => s.SaleOrigin)
                .Include(s => s.SaleStatus)
                .ToListAsync();
        }
    }
}