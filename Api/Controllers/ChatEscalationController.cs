using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatEscalation;
using Api.Hubs;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/chat/escalations")]
    public class ChatEscalationController : ControllerBase
    {
        private readonly IChatEscalationService _chatEscalationService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatEscalationController(
            IChatEscalationService chatEscalationService,
            IHubContext<ChatHub> hubContext)
        {
            _chatEscalationService = chatEscalationService;
            _hubContext = hubContext;
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetPending()
        {
            var escalations = await _chatEscalationService.GetPendingAsync();
            return Ok(escalations);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetById(int id)
        {
            var escalation = await _chatEscalationService.GetByIdAsync(id);
            return escalation is null ? NotFound() : Ok(escalation);
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> Create([FromBody] CreateChatEscalationRequest request)
        {
            request.NormalizeFromSnakeCase();
            try
            {
                var (escalation, created) = await _chatEscalationService.CreateAsync(request);

                if (created)
                    await _hubContext.Clients.Group("Advisors").SendAsync("NewEscalation", escalation);

                return CreatedAtAction(nameof(GetById), new { id = escalation.ChatEscalationId }, escalation);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/assign")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignChatEscalationRequest request)
        {
            var escalation = await _chatEscalationService.AssignAsync(id, request);
            return escalation is null ? NotFound() : Ok(escalation);
        }

        [HttpPatch("{id}/resolve")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> Resolve(int id)
        {
            try
            {
                var escalation = await _chatEscalationService.ResolveAsync(id);
                if (escalation is null) return NotFound();

                // Avisar al cliente en la sesión (FAB / chatbot)
                await _hubContext.Clients
                    .Group($"session-{escalation.ChatSessionId}")
                    .SendAsync("EscalationResolved", new
                    {
                        chatEscalationId = escalation.ChatEscalationId,
                        chatSessionId = escalation.ChatSessionId,
                        statusName = escalation.StatusName,
                        message = "El asesor marcó tu consulta como resuelta. Si necesitas algo más, escribe de nuevo o contacta soporte.",
                    });

                return Ok(escalation);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}