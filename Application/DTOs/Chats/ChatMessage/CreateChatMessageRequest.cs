namespace Application.DTOs.Chats.ChatMessage
{
    public class CreateChatMessageRequest
    {
        public int ChatSessionId { get; set; }
        public int SenderTypeId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}