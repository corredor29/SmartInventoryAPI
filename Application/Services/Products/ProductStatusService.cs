using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Products;
using Application.DTOs.Products.ProductStatus;
using Domain.Entities.Products;
using Domain.ValueObject.Products.ProductStatus;

namespace Application.Services.Products
{
    public class ProductStatusService : IProductStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ProductStatusDto>> GetAllAsync()
        {
            var statuses = await _unitOfWork.Repository<ProductStatus>().GetAllAsync();
            return statuses.Select(ToDto).ToList();
        }

        public async Task<ProductStatusDto?> GetByIdAsync(int id)
        {
            var status = await _unitOfWork.Repository<ProductStatus>().GetByIdAsync(id);
            return status is null ? null : ToDto(status);
        }

        public async Task<ProductStatusDto> CreateAsync(CreateProductStatusRequest request)
        {
            var status = new ProductStatus(ProductStatusName.Create(request.Name));

            await _unitOfWork.Repository<ProductStatus>().AddAsync(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }

        public async Task<ProductStatusDto?> UpdateAsync(int id, UpdateProductStatusRequest request)
        {
            var status = await _unitOfWork.Repository<ProductStatus>().GetByIdAsync(id);
            if (status is null) return null;

            status.Update(ProductStatusName.Create(request.Name));
            _unitOfWork.Repository<ProductStatus>().Update(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var status = await _unitOfWork.Repository<ProductStatus>().GetByIdAsync(id);
            if (status is null) return false;

            _unitOfWork.Repository<ProductStatus>().Remove(status);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ProductStatusDto ToDto(ProductStatus status) => new()
        {
            ProductStatusId = status.Id,
            Name = status.Name.Value,
        };
    }
}