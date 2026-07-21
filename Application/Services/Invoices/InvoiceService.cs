using System;
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

        public async Task<IReadOnlyList<InvoiceDto>> GetMineAsync(int authenticatedUserId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(authenticatedUserId);
            if (user is null)
                return Array.Empty<InvoiceDto>();

            var existingByEmail = await _unitOfWork.Customers.GetByEmailAsync(user.Email.Value);
            int? customerId = existingByEmail?.Id ?? user.CustomerId;

            if (existingByEmail is not null && user.CustomerId != existingByEmail.Id)
            {
                user.LinkCustomer(existingByEmail.Id);
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }

            if (!customerId.HasValue)
                return Array.Empty<InvoiceDto>();

            var invoices = await _unitOfWork.Invoices.GetByCustomerIdAsync(customerId.Value);
            var result = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                var dto = await BuildDtoAsync(invoice);
                if (dto != null) result.Add(dto);
            }

            return result;
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
                PaymentMethod = sale.PaymentMethod?.Value,
                DeliveryAddress = sale.DeliveryAddress,
                ContactPhone = sale.ContactPhone,
                ContactDocument = sale.ContactDocument,
                Items = sale.Details.Select(d => new InvoiceItemDto
                {
                    ProductName = d.Product?.Name.Value ?? string.Empty,
                    ImageUrl = d.Product?.ImageUrl,
                    CategoryName = d.Product?.Category?.Name.Value,
                    Quantity = d.Quantity.Value,
                    UnitPrice = d.UnitPrice.Value,
                    Subtotal = d.GetSubtotal(),
                }).ToList(),
            };
        }

        public async Task<(byte[] Content, string FileName)?> GeneratePdfAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            if (dto is null) return null;

            var bytes = InvoicePdfGenerator.Generate(dto);
            var safeName = dto.InvoiceNumber.Replace('/', '-');
            return (bytes, $"{safeName}.pdf");
        }
    }
}
