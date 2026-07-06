using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.SaleStatus;
using Domain.Entities.Sales;
using Domain.ValueObject.Sales.SaleStatus;

namespace Application.Services.Sales
{
    public class SaleStatusService : ISaleStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<SaleStatusDto>> GetAllAsync()
        {
            var statuses = await _unitOfWork.Repository<SaleStatus>().GetAllAsync();
            return statuses.Select(ToDto).ToList();
        }

        public async Task<SaleStatusDto?> GetByIdAsync(int id)
        {
            var status = await _unitOfWork.Repository<SaleStatus>().GetByIdAsync(id);
            return status is null ? null : ToDto(status);
        }
        public async Task<SaleStatusDto> CreateAsync(CreateSaleStatusRequest request)
        {
            var status = new SaleStatus(SaleStatusName.Create(request.Name));

            await _unitOfWork.Repository<SaleStatus>().AddAsync(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }

        public async Task<SaleStatusDto?> UpdateAsync(int id, UpdateSaleStatusRequest request)
        {
            var status = await _unitOfWork.Repository<SaleStatus>().GetByIdAsync(id);
            if (status is null) return null;

            status.UpdateName(SaleStatusName.Create(request.Name));
            _unitOfWork.Repository<SaleStatus>().Update(status);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(status);
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var status = await _unitOfWork.Repository<SaleStatus>().GetByIdAsync(id);
            if (status is null) return false;

            _unitOfWork.Repository<SaleStatus>().Remove(status);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static SaleStatusDto ToDto(SaleStatus status) => new()
        {
            SaleStatusId = status.Id,
            Name = status.Name.Value,
        };
    }
}