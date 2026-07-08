using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatSession;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/chat-sessions")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class ChatSessionController : ControllerBase
    {
        private readonly IChatSessionService _chatSessionService;

        public ChatSessionController(IChatSessionService chatSessionService)
        {
            _chatSessionService = chatSessionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sessions = await _chatSessionService.GetAllAsync();
            return Ok(sessions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var session = await _chatSessionService.GetByIdAsync(id);
            return session is null ? NotFound() : Ok(session);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] int chatSessionStatusId)
        {
            var session = await _chatSessionService.ChangeStatusAsync(id, chatSessionStatusId);
            return session is null ? NotFound() : Ok(session);
        }
    }
}