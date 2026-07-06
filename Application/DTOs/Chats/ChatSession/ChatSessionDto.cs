using System;

namespace Application.DTOs.Chats.ChatSession
{
    public class ChatSessionDto
    {
        public int ChatSessionId { get; set; }
        public int? CustomerId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
    }
}