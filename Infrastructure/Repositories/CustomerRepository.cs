using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Customers;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context) { }

        public async Task<Customer?> GetByDocumentNumberAsync(string documentNumber)
        {
            var customers = await DbSet.ToListAsync();
            return customers.Find(c => c.DocumentNumber != null && c.DocumentNumber.Value == documentNumber);
        }
    }
}