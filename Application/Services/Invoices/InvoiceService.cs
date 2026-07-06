using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Invoices;
using Application.DTOs.Invoices.Invoice;
using Domain.Entities.Invoices;

namespace Application.Services.Invoices
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InvoiceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<InvoiceDto>> GetAllAsync()
        {
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var result = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                var dto = await BuildDtoAsync(invoice);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        public async Task<InvoiceDto?> GetByIdAsync(int id)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
            return invoice is null ? null : await BuildDtoAsync(invoice);
        }

        public async Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            var invoice = await _unitOfWork.Invoices.GetByInvoiceNumberAsync(invoiceNumber);
            return invoice is null ? null : await BuildDtoAsync(invoice);
        }

        private async Task<InvoiceDto?> BuildDtoAsync(Invoice invoice)
        {
            var sale = await _unitOfWork.Sales.GetByIdWithDetailsAsync(invoice.SaleId);
            if (sale is null) return null;

            return new InvoiceDto
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber.Value,
                IssueDate = invoice.IssueDate.Value,
                SaleId = invoice.SaleId,
                CustomerName = sale.Customer?.Name.Value ?? string.Empty,
                Total = sale.GetTotal(),
                Items = sale.Details.Select(d => new InvoiceItemDto
                {
                    ProductName = d.Product?.Name.Value ?? string.Empty,
                    Quantity = d.Quantity.Value,
                    UnitPrice = d.UnitPrice.Value,
                    Subtotal = d.GetSubtotal(),
                }).ToList(),
            };
        }
    }
}