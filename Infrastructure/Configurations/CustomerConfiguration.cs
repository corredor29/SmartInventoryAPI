using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Customers;
using Domain.ValueObject.Customers.Customer;

namespace Infrastructure.Persistence.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customer");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("customer_id");

            builder.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired()
                .HasConversion(v => v.Value, v => new CustomerName(v));

            builder.Property(c => c.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new CustomerEmail(v));

            builder.Property(c => c.PhoneNumber)
                .HasColumnName("phone")
                .HasMaxLength(30)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new Phone(v));

            builder.Property(c => c.DocumentNumber)
                .HasColumnName("document_number")
                .HasMaxLength(50)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new DocumentNumber(v));

            builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        }
    }
}
