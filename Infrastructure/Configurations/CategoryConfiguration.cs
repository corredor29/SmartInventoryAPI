using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Products;
using Domain.ValueObject.Products.Category;

namespace Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("category");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("category_id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired()
            .HasConversion(v => v.Value, v => CategoryName.Create(v));

            builder.HasIndex(c => c.Name).IsUnique();
        }
    }
}
