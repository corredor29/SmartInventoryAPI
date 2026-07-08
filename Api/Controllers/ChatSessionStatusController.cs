using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatSessionStatus;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/chat-session-statuses")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class ChatSessionStatusController : ControllerBase
    {
        private readonly IChatSessionStatusService _chatSessionStatusService;

        public ChatSessionStatusController(IChatSessionStatusService chatSessionStatusService)
        {
            _chatSessionStatusService = chatSessionStatusService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var statuses = await _chatSessionStatusService.GetAllAsync();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var status = await _chatSessionStatusService.GetByIdAsync(id);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateChatSessionStatusRequest request)
        {
            var status = await _chatSessionStatusService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = status.ChatSessionStatusId }, status);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateChatSessionStatusRequest request)
        {
            var status = await _chatSessionStatusService.UpdateAsync(id, request);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _chatSessionStatusService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}