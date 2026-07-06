using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Sales.SaleDetail;

namespace Application.Contracts.Services.Sales
{
    public interface ISaleDetailService
    {
        Task<IReadOnlyList<SaleDetailDto>> GetBySaleIdAsync(int saleId);
    }
}