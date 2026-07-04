using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Inventories;
using Domain.ValueObject.Inventories.InventoryMovement;

namespace Infrastructure.Persistence.Configurations
{
    public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
    {
        public void Configure(EntityTypeBuilder<InventoryMovement> builder)
        {
            builder.ToTable("inventory_movement");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("movement_id");

            builder.Property(m => m.Quantity)
                .HasColumnName("quantity")
                .IsRequired()
                .HasConversion(v => v.Value, v => new MovementQuantity(v));

            builder.Property(m => m.Reason)
                .HasColumnName("reason")
                .HasMaxLength(255)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new MovementReason(v));

            builder.Property(m => m.CreatedAt).HasColumnName("created_at");
            builder.Property(m => m.InventoryId).HasColumnName("inventory_id");
            builder.Property(m => m.MovementTypeId).HasColumnName("movement_type_id");

            builder.HasOne(m => m.Inventory)
                .WithMany(i => i.Movements)
                .HasForeignKey(m => m.InventoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.MovementType)
                .WithMany(t => t.Movements)
                .HasForeignKey(m => m.MovementTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
