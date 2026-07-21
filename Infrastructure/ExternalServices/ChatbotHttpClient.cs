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
    /// <summary>
    /// Cliente HTTP hacia el chatbot externo (n8n local o FastAPI legacy).
    /// React nunca habla con este servicio: solo .NET.
    /// </summary>
    public class ChatbotHttpClient : IChatbotClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotHttpClient> _logger;
        private readonly TimeSpan _timeout;
        private readonly string _messagePath;

        public ChatbotHttpClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ChatbotHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(
                configuration["Chatbot:BaseUrl"] ?? "http://localhost:5678");

            var path = configuration["Chatbot:MessagePath"] ?? "/webhook/chat-message";
            _messagePath = path.StartsWith('/') ? path : "/" + path;

            var timeoutSeconds = 90;
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

                var response = await _httpClient.PostAsync(_messagePath, content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogWarning(
                        "Chatbot HTTP {StatusCode} en {Path}: {Body}",
                        (int)response.StatusCode,
                        _messagePath,
                        body);

                    return new ChatBotResponseDto
                    {
                        Response = "El asistente no esta disponible en este momento. Intenta de nuevo en unos minutos.",
                        State = "ERROR",
                    };
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Chatbot devolvio cuerpo vacio en {Path}", _messagePath);
                    return new ChatBotResponseDto
                    {
                        Response = "El asistente no devolvio una respuesta. Revisa que el workflow de n8n este activo.",
                        State = "ERROR",
                    };
                }

                using var doc = JsonDocument.Parse(json);
                var root = UnwrapRoot(doc.RootElement);

                var responseText = ReadString(root, "response", "Response");
                if (string.IsNullOrWhiteSpace(responseText)
                    || responseText.Contains("max iterations", StringComparison.OrdinalIgnoreCase)
                    || responseText.Contains("Sin respuesta del bot", StringComparison.OrdinalIgnoreCase))
                {
                    responseText =
                        "Estoy teniendo un problema tecnico momentaneo. " +
                        "Intenta de nuevo o reformula tu busqueda (ej. lenovo, laptop).";
                }

                return new ChatBotResponseDto
                {
                    Response = responseText,
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
                    Response = "El asistente tardo demasiado en responder. Intenta de nuevo en unos momentos.",
                    State = "ERROR",
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "No se pudo conectar con el chatbot en {BaseAddress}{Path}",
                    _httpClient.BaseAddress, _messagePath);
                return new ChatBotResponseDto
                {
                    Response = "No se pudo conectar con el asistente. Verifica que n8n este en ejecucion (Node).",
                    State = "ERROR",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al llamar al chatbot");
                return new ChatBotResponseDto
                {
                    Response = "Ocurrio un error al procesar tu mensaje. Intenta de nuevo.",
                    State = "ERROR",
                };
            }
        }

        private static JsonElement UnwrapRoot(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return root;
            if (TryGetPropertyIgnoreCase(root, "response", out _)) return root;

            foreach (var wrapper in new[] { "body", "data", "json" })
            {
                if (!TryGetPropertyIgnoreCase(root, wrapper, out var inner)) continue;

                if (inner.ValueKind == JsonValueKind.Object
                    && TryGetPropertyIgnoreCase(inner, "response", out _))
                    return inner;

                if (inner.ValueKind == JsonValueKind.String)
                {
                    try
                    {
                        using var nested = JsonDocument.Parse(inner.GetString() ?? "{}");
                        if (TryGetPropertyIgnoreCase(nested.RootElement, "response", out _))
                            return nested.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                    }
                }
            }

            return root;
        }

        private static string? ReadString(JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
            }

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

        private static System.Collections.Generic.List<ChatBotProductDto> ReadProducts(JsonElement root)
        {
            var list = new System.Collections.Generic.List<ChatBotProductDto>();
            if (!TryGetPropertyIgnoreCase(root, "products", out var productsEl)
                || productsEl.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var item in productsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var id = ReadInt(item, "product_id", "productId", "ProductId");
                if (id is null or <= 0) continue;

                list.Add(new ChatBotProductDto
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