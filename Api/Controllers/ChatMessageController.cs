using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Chats;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/chat-messages")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class ChatMessageController : ControllerBase
    {
        private readonly IChatMessageService _chatMessageService;

        public ChatMessageController(IChatMessageService chatMessageService)
        {
            _chatMessageService = chatMessageService;
        }

        [HttpGet("by-session/{chatSessionId}")]
        public async Task<IActionResult> GetBySessionId(int chatSessionId)
        {
            var messages = await _chatMessageService.GetBySessionIdAsync(chatSessionId);
            return Ok(messages);
        }
    }
}