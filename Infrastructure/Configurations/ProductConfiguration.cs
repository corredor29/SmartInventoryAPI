using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Products;
using Domain.ValueObject.Products.Product;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("product");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("product_id");

            builder.Property(p => p.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired()
                .HasConversion(v => v.Value, v => new ProductName(v));

            builder.Property(p => p.Description)
                .HasColumnName("description")
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new ProductDescription(v));

            builder.Property(p => p.Price)
                .HasColumnName("price")
                .HasColumnType("numeric(12,2)")
                .IsRequired()
                .HasConversion(v => v.Value, v => new ProductPrice(v));

            builder.Property(p => p.Embedding)
                .HasColumnName("embedding")
                .HasColumnType("vector(1536)");

            builder.Property(p => p.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(1000);

            builder.Property(p => p.CreatedAt).HasColumnName("created_at");
            builder.Property(p => p.CategoryId).HasColumnName("category_id");
            builder.Property(p => p.ProductStatusId).HasColumnName("product_status_id");

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ProductStatus)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.ProductStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.Embedding)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops");
        }
    }
}