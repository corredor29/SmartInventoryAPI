using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Products.ProductStatus;

namespace Application.Contracts.Services.Products
{
    public interface IProductStatusService
    {
        Task<IReadOnlyList<ProductStatusDto>> GetAllAsync();
        Task<ProductStatusDto?> GetByIdAsync(int id);
        Task<ProductStatusDto> CreateAsync(CreateProductStatusRequest request);
        Task<ProductStatusDto?> UpdateAsync(int id, UpdateProductStatusRequest request);
        Task<bool> DeleteAsync(int id);
    }
}