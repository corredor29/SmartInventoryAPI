using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.ChatSessionStatus;

namespace Infrastructure.Persistence.Configurations
{
    public class ChatSessionStatusConfiguration : IEntityTypeConfiguration<ChatSessionStatus>
    {
        public void Configure(EntityTypeBuilder<ChatSessionStatus> builder)
        {
            builder.ToTable("chat_session_status");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("chat_session_status_id");

            builder.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(v => v.Value, v => ChatSessionStatusName.Create(v));

            builder.HasIndex(c => c.Name).IsUnique();
        }
    }
}
