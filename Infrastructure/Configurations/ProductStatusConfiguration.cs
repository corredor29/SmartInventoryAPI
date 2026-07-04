using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Products;
using Domain.ValueObject.Products.ProductStatus;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductStatusConfiguration : IEntityTypeConfiguration<ProductStatus>
    {
        public void Configure(EntityTypeBuilder<ProductStatus> builder)
        {
            builder.ToTable("product_status");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("product_status_id");

            builder.Property(p => p.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => new ProductStatusName(v));

            builder.HasIndex(p => p.Name).IsUnique();
        }
    }
}
