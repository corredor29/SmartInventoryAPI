namespace Application.DTOs.Chats.Chat
{
    public class ChatBotResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public string State { get; set; } = "IN_PROGRESS";
        public string? InvoiceNumber { get; set; }
    }
}