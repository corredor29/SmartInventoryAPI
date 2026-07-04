using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Sales;
using Domain.ValueObject.Invoices.Invoice;
namespace Domain.Entities.Invoices
{
    public sealed class Invoice : BaseEntity
    {
        public int           SaleId        { get; private set; }
        public InvoiceNumber InvoiceNumber { get; private set; } = null!;
        public IssueDate     IssueDate     { get; private set; } = null!;

        public Sale Sale { get; private set; } = null!;

        private Invoice() { }

        public Invoice(int saleId, InvoiceNumber invoiceNumber, IssueDate? issueDate = null)
        {
            SaleId        = saleId > 0 ? saleId : throw new ArgumentException("SaleId must be greater than 0.");
            InvoiceNumber = invoiceNumber ?? throw new ArgumentNullException(nameof(invoiceNumber));
            IssueDate     = issueDate ?? IssueDate.Now();
        }

        public static Invoice GenerateFor(Sale sale, int sequence)
        {
            if (sale is null)
                throw new ArgumentNullException(nameof(sale));

            return new Invoice(sale.Id, InvoiceNumber.FromSequence(sequence));
        }
    }
}