using System.Threading.Tasks;
using Domain.Entities.Sales;

namespace Application.Contracts.Repositories
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<Sale?> GetByIdWithDetailsAsync(int saleId);
    }
}