using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Sales;
using Domain.ValueObject.Sales.Sale;

namespace Infrastructure.Persistence.Configurations
{
    public class SaleConfiguration : IEntityTypeConfiguration<Sale>
    {
        public void Configure(EntityTypeBuilder<Sale> builder)
        {
            builder.ToTable("sale");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("sale_id");

            builder.Property(s => s.SaleDate)
                .HasColumnName("sale_date")
                .IsRequired()
                .HasConversion(v => v.Value, v => new SaleDate(v));

            builder.Property(s => s.CustomerId).HasColumnName("customer_id");
            builder.Property(s => s.SaleOriginId).HasColumnName("sale_origin_id");
            builder.Property(s => s.SaleStatusId).HasColumnName("sale_status_id");

            builder.Property(s => s.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => PaymentMethod.Create(v));

            builder.Property(s => s.DeliveryAddress)
                .HasColumnName("delivery_address")
                .HasMaxLength(500);

            builder.Property(s => s.DeliveryLat)
                .HasColumnName("delivery_lat")
                .HasColumnType("numeric(10,7)");

            builder.Property(s => s.DeliveryLng)
                .HasColumnName("delivery_lng")
                .HasColumnType("numeric(10,7)");

            builder.Property(s => s.ContactPhone)
                .HasColumnName("contact_phone")
                .HasMaxLength(30);

            builder.Property(s => s.ContactDocument)
                .HasColumnName("contact_document")
                .HasMaxLength(50);

            // Backing field: la colección privada _details se mapea igual a través de la propiedad Details
            builder.Metadata.FindNavigation(nameof(Sale.Details))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasOne(s => s.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SaleOrigin)
                .WithMany(o => o.Sales)
                .HasForeignKey(s => s.SaleOriginId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SaleStatus)
                .WithMany(st => st.Sales)
                .HasForeignKey(s => s.SaleStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Details)
                .WithOne(d => d.Sale)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Invoice)
                .WithOne(i => i.Sale)
                .HasForeignKey<Domain.Entities.Invoices.Invoice>(i => i.SaleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
