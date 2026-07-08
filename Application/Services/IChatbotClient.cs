using System.Threading.Tasks;
using Application.DTOs.Chats.Chat;

namespace Application.Contracts.Services
{
    public interface IChatbotClient
    {
        Task<ChatBotResponseDto> SendMessageAsync(string sessionId, string message);
    }
}