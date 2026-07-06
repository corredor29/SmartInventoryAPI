using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Products;
using Application.DTOs.Products.Product;
using Domain.Entities.Inventories;
using Domain.Entities.Products;
using Domain.ValueObject.Products.Product;

namespace Application.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ProductDto>> GetAllAsync()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            return product is null ? null : ToDto(product);
        }

        public async Task<IReadOnlyList<ProductDto>> SearchAsync(string query)
        {
            var products = await _unitOfWork.Products.SearchAsync(query);
            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto> CreateAsync(CreateProductRequest request)
        {
            var product = new Product(
                categoryId: request.CategoryId,
                productStatusId: request.ProductStatusId,
                name: new ProductName(request.Name),
                price: new ProductPrice(request.Price),
                description: request.Description is null ? null : new ProductDescription(request.Description)
            );

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Todo producto nuevo nace con su registro de Inventory en 0
            var inventory = new Inventory(product.Id);
            await _unitOfWork.Inventory.AddAsync(inventory);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(product);
        }

        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return null;

            product.Update(
                name: new ProductName(request.Name),
                price: new ProductPrice(request.Price),
                description: request.Description is null ? null : new ProductDescription(request.Description),
                categoryId: request.CategoryId
            );

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(product);
        }

        public async Task<ProductDto?> ChangeStatusAsync(int id, int productStatusId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return null;

            product.ChangeStatus(productStatusId);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(product);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return false;

            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ProductDto ToDto(Product product) => new()
        {
            ProductId = product.Id,
            Name = product.Name.Value,
            Description = product.Description?.Value,
            Price = product.Price.Value,
            CategoryName = product.Category?.Name.Value ?? string.Empty,
            StatusName = product.ProductStatus?.Name.Value ?? string.Empty,
            CurrentStock = product.Inventory?.CurrentStock.Value ?? 0,
        };
    }
}