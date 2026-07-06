using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Products.Product;

namespace Application.Contracts.Services.Products
{
    public interface IProductService
    {
        Task<IReadOnlyList<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(int id);
        Task<IReadOnlyList<ProductDto>> SearchAsync(string query);
        Task<ProductDto> CreateAsync(CreateProductRequest request);
        Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request);
        Task<ProductDto?> ChangeStatusAsync(int id, int productStatusId);
        Task<bool> DeleteAsync(int id);
    }
}