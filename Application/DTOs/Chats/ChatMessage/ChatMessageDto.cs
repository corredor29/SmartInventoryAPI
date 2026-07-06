using System;

namespace Application.DTOs.Chats.ChatMessage
{
    public class ChatMessageDto
    {
        public int ChatMessageId { get; set; }
        public int ChatSessionId { get; set; }
        public string SenderTypeName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}