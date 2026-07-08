namespace Application.DTOs.Chats.AdvisorNotification
{
    public class AdvisorNotificationRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int? SaleId { get; set; }
    }
}