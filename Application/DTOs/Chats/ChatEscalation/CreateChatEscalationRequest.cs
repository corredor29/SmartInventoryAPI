namespace Application.DTOs.Chats.ChatEscalation
{
    public class CreateChatEscalationRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}