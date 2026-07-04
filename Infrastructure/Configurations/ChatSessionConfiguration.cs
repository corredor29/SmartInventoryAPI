using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.ChatSession;

namespace Infrastructure.Persistence.Configurations
{
    public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
    {
        public void Configure(EntityTypeBuilder<ChatSession> builder)
        {
            builder.ToTable("chat_session");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("chat_session_id");

            builder.Property(c => c.StartedAt)
                .HasColumnName("started_at")
                .IsRequired()
                .HasConversion(v => v.Value, v => new StartedAt(v));

            builder.Property(c => c.CustomerId).HasColumnName("customer_id");
            builder.Property(c => c.ChatSessionStatusId).HasColumnName("chat_session_status_id");

            builder.Metadata.FindNavigation(nameof(ChatSession.Messages))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasOne(c => c.Customer)
                .WithMany(cu => cu.ChatSessions)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.ChatSessionStatus)
                .WithMany(s => s.ChatSessions)
                .HasForeignKey(c => c.ChatSessionStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Messages)
                .WithOne(m => m.ChatSession)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Escalation)
                .WithOne(e => e.ChatSession)
                .HasForeignKey<ChatEscalation>(e => e.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
