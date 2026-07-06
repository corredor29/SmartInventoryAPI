using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Products;

namespace Application.Contracts.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IReadOnlyList<Product>> SearchAsync(string query);

        Task<Product?> GetByIdWithInventoryAsync(int productId);
    }
}