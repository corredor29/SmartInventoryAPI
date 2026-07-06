using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Chats.ChatSessionStatus;

namespace Application.Contracts.Services.Chats
{
    public interface IChatSessionStatusService
    {
        Task<IReadOnlyList<ChatSessionStatusDto>> GetAllAsync();
        Task<ChatSessionStatusDto?> GetByIdAsync(int id);
        Task<ChatSessionStatusDto> CreateAsync(CreateChatSessionStatusRequest request);
        Task<ChatSessionStatusDto?> UpdateAsync(int id, UpdateChatSessionStatusRequest request);
        Task<bool> DeleteAsync(int id);
    }
}