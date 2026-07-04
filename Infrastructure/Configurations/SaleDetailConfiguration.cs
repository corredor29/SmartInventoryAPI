using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Sales;
using Domain.ValueObject.Sales.SaleDetail;

namespace Infrastructure.Persistence.Configurations
{
    public class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
    {
        public void Configure(EntityTypeBuilder<SaleDetail> builder)
        {
            builder.ToTable("sale_detail");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id).HasColumnName("sale_detail_id");

            builder.Property(d => d.Quantity)
                .HasColumnName("quantity")
                .IsRequired()
                .HasConversion(v => v.Value, v => new SaleDetailQuantity(v));

            builder.Property(d => d.UnitPrice)
                .HasColumnName("unit_price")
                .HasColumnType("numeric(12,2)")
                .IsRequired()
                .HasConversion(v => v.Value, v => new UnitPrice(v));

            builder.Property(d => d.SaleId).HasColumnName("sale_id");
            builder.Property(d => d.ProductId).HasColumnName("product_id");

            builder.HasOne(d => d.Product)
                .WithMany(p => p.SaleDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
