using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices
{
    public sealed class OpenAiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiEmbeddingService> _logger;
        private readonly string? _apiKey;
        private readonly string _model;

        public OpenAiEmbeddingService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAiEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(_apiKey))
                _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            _model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
            _httpClient.BaseAddress ??= new Uri("https://api.openai.com/v1/");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<float[]?> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                return null;

            var input = (text ?? string.Empty).Trim();
            if (input.Length == 0)
                return null;

            if (input.Length > 8000)
                input = input[..8000];

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = JsonContent.Create(new
                {
                    model = _model,
                    input,
                });

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "OpenAI embeddings fallo ({Status}): {Body}",
                        (int)response.StatusCode,
                        body.Length > 300 ? body[..300] : body);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
                var values = payload?.Data?.FirstOrDefault()?.Embedding;
                if (values is null || values.Count == 0)
                    return null;

                return values.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo generar embedding para el texto.");
                return null;
            }
        }

        private sealed class EmbeddingResponse
        {
            [JsonPropertyName("data")]
            public List<EmbeddingItem>? Data { get; set; }
        }

        private sealed class EmbeddingItem
        {
            [JsonPropertyName("embedding")]
            public List<float>? Embedding { get; set; }
        }
    }
}
