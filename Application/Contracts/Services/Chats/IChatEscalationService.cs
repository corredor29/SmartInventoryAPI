using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Chats.ChatEscalation;

namespace Application.Contracts.Services.Chats
{
    public interface IChatEscalationService
    {
        Task<IReadOnlyList<ChatEscalationDto>> GetPendingAsync();
        Task<ChatEscalationDto?> GetByIdAsync(int id);
        Task<ChatEscalationDto> CreateAsync(CreateChatEscalationRequest request);
        Task<ChatEscalationDto?> AssignAsync(int id, AssignChatEscalationRequest request);
        Task<ChatEscalationDto?> ResolveAsync(int id);
    }
}