using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Products.Category;

namespace Application.Contracts.Services.Products
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
        Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request);
        Task<bool> DeleteAsync(int id);
    }
}