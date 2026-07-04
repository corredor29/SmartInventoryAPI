using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.ChatEscalation;

namespace Infrastructure.Persistence.Configurations
{
    public class ChatEscalationConfiguration : IEntityTypeConfiguration<ChatEscalation>
    {
        public void Configure(EntityTypeBuilder<ChatEscalation> builder)
        {
            builder.ToTable("chat_escalation");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("chat_escalation_id");

            builder.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new EscalationReason(v));

            builder.Property(e => e.ResolvedAt)
                .HasColumnName("resolved_at")
                .HasConversion(
                    v => v == null ? (DateTime?)null : v.Value,
                    v => v == null ? null : new ResolvedAt(v.Value));

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.ChatSessionId).HasColumnName("chat_session_id");
            builder.Property(e => e.EscalationStatusId).HasColumnName("escalation_status_id");
            builder.Property(e => e.AssignedUserId).HasColumnName("assigned_user_id");

            builder.HasIndex(e => e.ChatSessionId).IsUnique();

            builder.HasOne(e => e.EscalationStatus)
                .WithMany(s => s.Escalations)
                .HasForeignKey(e => e.EscalationStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AssignedUser)
                .WithMany(u => u.AssignedEscalations)
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
