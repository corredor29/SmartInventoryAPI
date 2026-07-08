using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Application.DTOs.Chats.AdvisorNotification;
using Api.Hubs;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class AdvisorNotificationController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public AdvisorNotificationController(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("notify-advisor")]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> NotifyAdvisor([FromBody] AdvisorNotificationRequest request)
        {
            await _hubContext.Clients.Group("Advisors").SendAsync("AdvisorNotification", request);
            return Ok(new { success = true });
        }
    }
}