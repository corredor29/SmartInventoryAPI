using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Inventories;
using Domain.ValueObject.Inventories.MovementType;

namespace Infrastructure.Persistence.Configurations
{
    public class MovementTypeConfiguration : IEntityTypeConfiguration<MovementType>
    {
        public void Configure(EntityTypeBuilder<MovementType> builder)
        {
            builder.ToTable("movement_type");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("movement_type_id");

            builder.Property(m => m.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => new MovementTypeName(v));

            builder.HasIndex(m => m.Name).IsUnique();
        }
    }
}
