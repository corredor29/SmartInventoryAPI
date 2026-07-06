using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Sales;
using Domain.ValueObject.Sales.SaleOrigin;

namespace Infrastructure.Persistence.Configurations
{
    public class SaleOriginConfiguration : IEntityTypeConfiguration<SaleOrigin>
    {
        public void Configure(EntityTypeBuilder<SaleOrigin> builder)
        {
            builder.ToTable("sale_origin");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("sale_origin_id");

            builder.Property(s => s.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => SaleOriginName.Create(v));

            builder.HasIndex(s => s.Name).IsUnique();
        }
    }
}
