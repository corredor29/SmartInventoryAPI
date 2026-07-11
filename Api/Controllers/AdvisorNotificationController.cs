using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
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
        public async Task<IActionResult> NotifyAdvisor([FromBody] JsonElement body)
        {
            static string? ReadString(JsonElement el, params string[] names)
            {
                foreach (var name in names)
                {
                    if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                        return p.GetString();
                }
                foreach (var prop in el.EnumerateObject())
                {
                    foreach (var name in names)
                    {
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)
                            && prop.Value.ValueKind == JsonValueKind.String)
                            return prop.Value.GetString();
                    }
                }
                return null;
            }

            static int? ReadInt(JsonElement el, params string[] names)
            {
                foreach (var name in names)
                {
                    if (el.TryGetProperty(name, out var p) && p.TryGetInt32(out var v))
                        return v;
                }
                foreach (var prop in el.EnumerateObject())
                {
                    foreach (var name in names)
                    {
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)
                            && prop.Value.TryGetInt32(out var v))
                            return v;
                    }
                }
                return null;
            }

            var payload = new
            {
                sessionId = ReadString(body, "sessionId", "session_id") ?? string.Empty,
                notificationType = ReadString(body, "notificationType", "notification_type") ?? string.Empty,
                details = ReadString(body, "details") ?? string.Empty,
                saleId = ReadInt(body, "saleId", "sale_id"),
            };

            await _hubContext.Clients.Group("Advisors").SendAsync("AdvisorNotification", payload);
            return Ok(new { success = true });
        }
    }
}
