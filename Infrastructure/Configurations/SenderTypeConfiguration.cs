using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.SenderType;

namespace Infrastructure.Persistence.Configurations
{
    public class SenderTypeConfiguration : IEntityTypeConfiguration<SenderType>
    {
        public void Configure(EntityTypeBuilder<SenderType> builder)
        {
            builder.ToTable("sender_type");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("sender_type_id");

            builder.Property(s => s.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => new SenderTypeName(v));

            builder.HasIndex(s => s.Name).IsUnique();
        }
    }
}
