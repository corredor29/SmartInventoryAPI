using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Products;
using Application.DTOs.Products.Category;
using Domain.Entities.Products;
using Domain.ValueObject.Products.Category;

namespace Application.Services.Products
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
        {
            var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
            return categories.Select(ToDto).ToList();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
            return category is null ? null : ToDto(category);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
        {
            var category = new Category(CategoryName.Create(request.Name));

            await _unitOfWork.Repository<Category>().AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(category);
        }

        public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
            if (category is null) return null;

            category.Update(CategoryName.Create(request.Name));
            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
            if (category is null) return false;

            _unitOfWork.Repository<Category>().Remove(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static CategoryDto ToDto(Category category) => new()
        {
            CategoryId = category.Id,
            Name = category.Name.Value,
        };
    }
}