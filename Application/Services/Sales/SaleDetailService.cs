using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.SaleDetail;
using Domain.Entities.Sales;

namespace Application.Services.Sales
{
    public class SaleDetailService : ISaleDetailService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleDetailService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<SaleDetailDto>> GetBySaleIdAsync(int saleId)
        {
            var sale = await _unitOfWork.Sales.GetByIdWithDetailsAsync(saleId);

            if (sale is null)
                return new List<SaleDetailDto>();

            return sale.Details.Select(d => new SaleDetailDto
            {
                SaleDetailId = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product?.Name.Value ?? string.Empty,
                Quantity = d.Quantity.Value,
                UnitPrice = d.UnitPrice.Value,
                Subtotal = d.GetSubtotal(),
            }).ToList();
        }
    }
}