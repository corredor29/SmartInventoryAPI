using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Sales.SaleOrigin;

namespace Application.Contracts.Services.Sales
{
    public interface ISaleOriginService
    {
        Task<IReadOnlyList<SaleOriginDto>> GetAllAsync();
        Task<SaleOriginDto?> GetByIdAsync(int id);
        Task<SaleOriginDto> CreateAsync(CreateSaleOriginRequest request);
        Task<SaleOriginDto?> UpdateAsync(int id, UpdateSaleOriginRequest request);
        Task<bool> DeleteAsync(int id);
    }
}