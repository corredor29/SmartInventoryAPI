using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Invoices;
using Domain.ValueObject.Invoices.Invoice;

namespace Infrastructure.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("invoice");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).HasColumnName("invoice_id");

            builder.Property(i => i.InvoiceNumber)
                .HasColumnName("invoice_number")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(v => v.Value, v => new InvoiceNumber(v));

            builder.Property(i => i.IssueDate)
                .HasColumnName("issue_date")
                .IsRequired()
                .HasConversion(v => v.Value, v => new IssueDate(v));

            builder.Property(i => i.SaleId).HasColumnName("sale_id");

            builder.HasIndex(i => i.InvoiceNumber).IsUnique();
            builder.HasIndex(i => i.SaleId).IsUnique();
        }
    }
}
