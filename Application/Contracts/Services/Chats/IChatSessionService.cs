using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Chats.ChatSession;

namespace Application.Contracts.Services.Chats
{
    public interface IChatSessionService
    {
        Task<IReadOnlyList<ChatSessionDto>> GetAllAsync();
        Task<ChatSessionDto?> GetByIdAsync(int id);
        Task<ChatSessionDto> CreateAsync(CreateChatSessionRequest request);
        Task<ChatSessionDto?> ChangeStatusAsync(int id, int chatSessionStatusId);
    }
}