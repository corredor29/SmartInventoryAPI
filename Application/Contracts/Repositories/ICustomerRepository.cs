using System.Threading.Tasks;
using Domain.Entities.Customers;

namespace Application.Contracts.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetByDocumentNumberAsync(string documentNumber);
    }
}