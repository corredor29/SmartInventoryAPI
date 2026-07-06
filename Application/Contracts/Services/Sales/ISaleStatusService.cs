using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Sales.SaleStatus;

namespace Application.Contracts.Services.Sales
{
    public interface ISaleStatusService
    {
        Task<IReadOnlyList<SaleStatusDto>> GetAllAsync();
        Task<SaleStatusDto?> GetByIdAsync(int id);
        Task<SaleStatusDto> CreateAsync(CreateSaleStatusRequest request);
        Task<SaleStatusDto?> UpdateAsync(int id, UpdateSaleStatusRequest request);
        Task<bool> DeleteAsync(int id);
    }
}