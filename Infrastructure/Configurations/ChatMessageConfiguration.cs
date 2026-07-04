using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.ChatMessage;

namespace Infrastructure.Persistence.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("chat_message");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("chat_message_id");

            builder.Property(m => m.Content)
                .HasColumnName("content")
                .IsRequired()
                .HasConversion(v => v.Value, v => new MessageContent(v));

            builder.Property(m => m.SentAt)
                .HasColumnName("sent_at")
                .IsRequired()
                .HasConversion(v => v.Value, v => new SentAt(v));

            builder.Property(m => m.ChatSessionId).HasColumnName("chat_session_id");
            builder.Property(m => m.SenderTypeId).HasColumnName("sender_type_id");

            builder.HasOne(m => m.SenderType)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SenderTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
