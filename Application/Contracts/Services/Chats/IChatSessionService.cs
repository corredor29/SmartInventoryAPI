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
        /// <summary>Asocia un Customer a la sesión si aún no tiene uno.</summary>
        Task<ChatSessionDto?> LinkCustomerAsync(int sessionId, int customerId);
    }
}