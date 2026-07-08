using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Application.Contracts.Services;
using Application.DTOs.Chats.Chat;

namespace Infrastructure.ExternalServices
{
    public class FastApiChatbotClient : IChatbotClient
    {
        private readonly HttpClient _httpClient;

        public FastApiChatbotClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configuration["Chatbot:BaseUrl"] ?? "http://localhost:8000");
        }

        public async Task<ChatBotResponseDto> SendMessageAsync(string sessionId, string message)
        {
            var payload = new { session_id = sessionId, message };

            var response = await _httpClient.PostAsJsonAsync("/chat/message", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatBotResponseDto>();
            return result ?? new ChatBotResponseDto { Response = "Sin respuesta del bot." };
        }
    }
}