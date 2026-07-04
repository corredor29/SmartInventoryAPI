using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Inventories;
using Domain.ValueObject.Inventories.Inventory;

namespace Infrastructure.Persistence.Configurations
{
    public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
    {
        public void Configure(EntityTypeBuilder<Inventory> builder)
        {
            builder.ToTable("inventory");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).HasColumnName("inventory_id");

            builder.Property(i => i.CurrentStock)
                .HasColumnName("current_stock")
                .IsRequired()
                .HasConversion(v => v.Value, v => new StockQuantity(v));

            builder.Property(i => i.ProductId).HasColumnName("product_id");

            builder.HasIndex(i => i.ProductId).IsUnique();

            builder.HasOne(i => i.Product)
                .WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
