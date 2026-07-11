using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Application.Contracts.Services;
using Application.DTOs.Chats.Chat;

namespace Infrastructure.ExternalServices
{
    public class FastApiChatbotClient : IChatbotClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<FastApiChatbotClient> _logger;
        private readonly TimeSpan _timeout;

        public FastApiChatbotClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<FastApiChatbotClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(configuration["Chatbot:BaseUrl"] ?? "http://localhost:8000");

            var timeoutSeconds = 60;
            if (int.TryParse(configuration["Chatbot:TimeoutSeconds"], out var configured) && configured > 0)
                timeoutSeconds = configured;
            _timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public async Task<ChatBotResponseDto> SendMessageAsync(string sessionId, string message)
        {
            var payload = new { session_id = sessionId, message };
            using var cts = new CancellationTokenSource(_timeout);

            try
            {
                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("/chat/message", content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogWarning(
                        "Chatbot HTTP {StatusCode}: {Body}",
                        (int)response.StatusCode,
                        body);

                    return new ChatBotResponseDto
                    {
                        Response = "El asistente no está disponible en este momento. Intenta de nuevo en unos minutos.",
                        State = "ERROR",
                    };
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
                var root = doc.RootElement;

                return new ChatBotResponseDto
                {
                    Response = ReadString(root, "response", "Response")
                               ?? "Sin respuesta del bot.",
                    State = ReadString(root, "state", "State") ?? "IN_PROGRESS",
                    InvoiceNumber = ReadString(root, "invoice_number", "invoiceNumber", "InvoiceNumber"),
                    SaleOrigin = ReadString(root, "sale_origin", "saleOrigin", "SaleOrigin"),
                    Products = ReadProducts(root),
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout esperando respuesta del chatbot (session {SessionId})", sessionId);
                return new ChatBotResponseDto
                {
                    Response = "El asistente tardó demasiado en responder. Intenta de nuevo en unos momentos.",
                    State = "ERROR",
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "No se pudo conectar con el chatbot en {BaseAddress}", _httpClient.BaseAddress);
                return new ChatBotResponseDto
                {
                    Response = "No se pudo conectar con el asistente. Verifica que el servicio del chatbot esté en ejecución.",
                    State = "ERROR",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al llamar al chatbot");
                return new ChatBotResponseDto
                {
                    Response = "Ocurrió un error al procesar tu mensaje. Intenta de nuevo.",
                    State = "ERROR",
                };
            }
        }

        private static string? ReadString(JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
            }

            // Case-insensitive fallback
            foreach (var prop in root.EnumerateObject())
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

        private static System.Collections.Generic.List<Application.DTOs.Chats.Chat.ChatBotProductDto> ReadProducts(JsonElement root)
        {
            var list = new System.Collections.Generic.List<Application.DTOs.Chats.Chat.ChatBotProductDto>();
            if (!TryGetPropertyIgnoreCase(root, "products", out var productsEl)
                || productsEl.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var item in productsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var id = ReadInt(item, "product_id", "productId", "ProductId");
                if (id is null or <= 0) continue;

                list.Add(new Application.DTOs.Chats.Chat.ChatBotProductDto
                {
                    ProductId = id.Value,
                    Name = ReadString(item, "name", "Name") ?? string.Empty,
                    Description = ReadString(item, "description", "Description"),
                    Price = ReadDecimal(item, "price", "Price") ?? 0,
                    CategoryName = ReadString(item, "category_name", "categoryName", "CategoryName") ?? string.Empty,
                    StatusName = ReadString(item, "status_name", "statusName", "StatusName") ?? string.Empty,
                    CurrentStock = ReadInt(item, "current_stock", "currentStock", "CurrentStock") ?? 0,
                    ImageUrl = ReadString(item, "image_url", "imageUrl", "ImageUrl"),
                });
            }

            return list;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
        {
            if (root.TryGetProperty(name, out value)) return true;
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        private static int? ReadInt(JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyIgnoreCase(root, name, out var prop)) continue;
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n)) return n;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsed))
                    return parsed;
            }
            return null;
        }

        private static decimal? ReadDecimal(JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyIgnoreCase(root, name, out var prop)) continue;
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var n)) return n;
                if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var parsed))
                    return parsed;
            }
            return null;
        }
    }
}
