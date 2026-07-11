namespace Application.DTOs.Chats.Chat
{
    public class ChatBotProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class ChatBotResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public string State { get; set; } = "IN_PROGRESS";
        public string? InvoiceNumber { get; set; }
        /// <summary>Origen de la venta cuando state = SALE_COMPLETED (ej. CHATBOT).</summary>
        public string? SaleOrigin { get; set; }
        public System.Collections.Generic.List<ChatBotProductDto> Products { get; set; } = new();
    }
}
