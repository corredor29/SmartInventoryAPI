using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Sales;
using Domain.ValueObject.Sales.SaleStatus;

namespace Infrastructure.Persistence.Configurations
{
    public class SaleStatusConfiguration : IEntityTypeConfiguration<SaleStatus>
    {
        public void Configure(EntityTypeBuilder<SaleStatus> builder)
        {
            builder.ToTable("sale_status");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("sale_status_id");

            builder.Property(s => s.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => SaleStatusName.Create(v));

            builder.HasIndex(s => s.Name).IsUnique();
        }
    }
}
