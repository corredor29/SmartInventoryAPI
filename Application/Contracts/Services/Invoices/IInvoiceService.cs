using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Invoices.Invoice;

namespace Application.Contracts.Services.Invoices
{
    public interface IInvoiceService
    {
        Task<IReadOnlyList<InvoiceDto>> GetAllAsync();
        Task<InvoiceDto?> GetByIdAsync(int id);
        Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<IReadOnlyList<InvoiceDto>> GetMineAsync(int authenticatedUserId);
        Task<(byte[] Content, string FileName)?> GeneratePdfAsync(int id);
    }
}