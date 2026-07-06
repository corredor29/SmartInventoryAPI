using System;

namespace Application.DTOs.Chats.ChatEscalation
{
    public class ChatEscalationDto
    {
        public int ChatEscalationId { get; set; }
        public int ChatSessionId { get; set; }
        public string? Reason { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? AssignedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}