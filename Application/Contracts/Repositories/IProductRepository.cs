using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Products;

namespace Application.Contracts.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IReadOnlyList<Product>> GetAllWithDetailsAsync();

        Task<IReadOnlyList<Product>> SearchAsync(string query);

        /// <summary>
        /// Busqueda semantica por distancia coseno (pgvector). Requiere embeddings poblados.
        /// </summary>
        Task<IReadOnlyList<Product>> SearchByEmbeddingAsync(float[] embedding, int take = 15);

        Task<Product?> GetByIdWithInventoryAsync(int productId);
    }
}
