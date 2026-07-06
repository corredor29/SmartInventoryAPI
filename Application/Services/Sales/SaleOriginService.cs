using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.SaleOrigin;
using Domain.Entities.Sales;
using Domain.ValueObject.Sales.SaleOrigin;

namespace Application.Services.Sales
{
    public class SaleOriginService : ISaleOriginService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleOriginService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<SaleOriginDto>> GetAllAsync()
        {
            var origins = await _unitOfWork.Repository<SaleOrigin>().GetAllAsync();
            return origins.Select(ToDto).ToList();
        }

        public async Task<SaleOriginDto?> GetByIdAsync(int id)
        {
            var origin = await _unitOfWork.Repository<SaleOrigin>().GetByIdAsync(id);
            return origin is null ? null : ToDto(origin);
        }

        public async Task<SaleOriginDto> CreateAsync(CreateSaleOriginRequest request)
        {
            var origin = new SaleOrigin(SaleOriginName.Create(request.Name));

            await _unitOfWork.Repository<SaleOrigin>().AddAsync(origin);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(origin);
        }

        public async Task<SaleOriginDto?> UpdateAsync(int id, UpdateSaleOriginRequest request)
        {
            var origin = await _unitOfWork.Repository<SaleOrigin>().GetByIdAsync(id);
            if (origin is null) return null;

            origin.UpdateName(SaleOriginName.Create(request.Name));
            _unitOfWork.Repository<SaleOrigin>().Update(origin);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(origin);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var origin = await _unitOfWork.Repository<SaleOrigin>().GetByIdAsync(id);
            if (origin is null) return false;

            _unitOfWork.Repository<SaleOrigin>().Remove(origin);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static SaleOriginDto ToDto(SaleOrigin origin) => new()
        {
            SaleOriginId = origin.Id,
            Name = origin.Name.Value,
        };
    }
}