using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Chats.ChatMessage;

namespace Application.Contracts.Services.Chats
{
    public interface IChatMessageService
    {
        Task<IReadOnlyList<ChatMessageDto>> GetBySessionIdAsync(int chatSessionId);
        Task<ChatMessageDto> CreateAsync(CreateChatMessageRequest request);
    }
}