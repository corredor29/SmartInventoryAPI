using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.EscalationStatus;

namespace Infrastructure.Persistence.Configurations
{
    public class EscalationStatusConfiguration : IEntityTypeConfiguration<EscalationStatus>
    {
        public void Configure(EntityTypeBuilder<EscalationStatus> builder)
        {
            builder.ToTable("escalation_status");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("escalation_status_id");

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => new EscalationStatusName(v));

            builder.HasIndex(e => e.Name).IsUnique();
        }
    }
}
