using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Sales.Sale;

namespace Application.Contracts.Services.Sales
{
    public interface ISaleService
    {
        Task<IReadOnlyList<SaleDto>> GetAllAsync();
        Task<SaleDto?> GetByIdAsync(int id);
        Task<SaleResultDto> CreateAsync(CreateSaleRequest request);
        Task<SaleDto?> ChangeStatusAsync(int id, int saleStatusId);
    }
}