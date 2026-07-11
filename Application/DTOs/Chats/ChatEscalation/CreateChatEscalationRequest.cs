using System.Collections.Generic;
using System.Text.Json;

namespace Application.DTOs.Chats.ChatEscalation
{
    /// <summary>
    /// Acepta camelCase (sessionId) y snake_case (session_id) vía JsonExtensionData.
    /// </summary>
    public class CreateChatEscalationRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        public void NormalizeFromSnakeCase()
        {
            if (ExtensionData is null) return;

            if (string.IsNullOrWhiteSpace(SessionId)
                && ExtensionData.TryGetValue("session_id", out var sid)
                && sid.ValueKind == JsonValueKind.String)
            {
                SessionId = sid.GetString() ?? string.Empty;
            }
        }
    }
}
