using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Contracts.Services.Chats;
using Application.Contracts.Services;
using Application.DTOs.Chats.ChatSession;
using Application.DTOs.Chats.ChatMessage;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatSessionService _chatSessionService;
        private readonly IChatMessageService _chatMessageService;
        private readonly IChatbotClient _chatbotClient;

        public ChatController(
            IChatSessionService chatSessionService,
            IChatMessageService chatMessageService,
            IChatbotClient chatbotClient)
        {
            _chatSessionService = chatSessionService;
            _chatMessageService = chatMessageService;
            _chatbotClient = chatbotClient;
        }
        [HttpPost("message")]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            // 1. Resolver o crear la sesión de chat
            ChatSessionDto session;
            if (int.TryParse(request.SessionId, out var sessionId))
            {
                var existing = await _chatSessionService.GetByIdAsync(sessionId);
                session = existing ?? await _chatSessionService.CreateAsync(new CreateChatSessionRequest());
            }
            else
            {
                session = await _chatSessionService.CreateAsync(new CreateChatSessionRequest());
            }

            // 2. Guardar el mensaje del cliente (SenderTypeId=2 asumido como "Cliente" según seed)
            await _chatMessageService.CreateAsync(new CreateChatMessageRequest
            {
                ChatSessionId = session.ChatSessionId,
                SenderTypeId = 2,
                Content = request.Message,
            });

            // 3. Reenviar a FastAPI
            var botResponse = await _chatbotClient.SendMessageAsync(session.ChatSessionId.ToString(), request.Message);

            // 4. Guardar la respuesta del bot (SenderTypeId=1 asumido como "Bot" según seed)
            await _chatMessageService.CreateAsync(new CreateChatMessageRequest
            {
                ChatSessionId = session.ChatSessionId,
                SenderTypeId = 1,
                Content = botResponse.Response,
            });

            return Ok(new
            {
                sessionId = session.ChatSessionId.ToString(),
                response = botResponse.Response,
                state = botResponse.State,
                invoiceNumber = botResponse.InvoiceNumber,
            });
        }
    }

    public class ChatMessageRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}