using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Invoices;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(AppDbContext context) : base(context) { }

        public async Task<int> GetNextSequenceAsync()
        {
            var count = await DbSet.CountAsync();
            return count + 1;
        }

        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            var invoices = await DbSet.Include(i => i.Sale).ToListAsync();
            return invoices.Find(i => i.InvoiceNumber.Value == invoiceNumber);
        }

        public async Task<IReadOnlyList<Invoice>> GetByCustomerIdAsync(int customerId)
        {
            return await DbSet
                .Include(i => i.Sale)
                .Where(i => i.Sale.CustomerId == customerId)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
        }
    }
}
