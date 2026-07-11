using System.Threading.Tasks;
using System.Collections.Generic;
using Domain.Entities.Invoices;

namespace Application.Contracts.Repositories
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        Task<int> GetNextSequenceAsync();
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<IReadOnlyList<Invoice>> GetByCustomerIdAsync(int customerId);
    }
}