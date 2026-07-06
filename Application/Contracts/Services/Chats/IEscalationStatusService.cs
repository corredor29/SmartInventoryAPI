using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Chats.EscalationStatus;

namespace Application.Contracts.Services.Chats
{
    public interface IEscalationStatusService
    {
        Task<IReadOnlyList<EscalationStatusDto>> GetAllAsync();
        Task<EscalationStatusDto?> GetByIdAsync(int id);
        Task<EscalationStatusDto> CreateAsync(CreateEscalationStatusRequest request);
        Task<EscalationStatusDto?> UpdateAsync(int id, UpdateEscalationStatusRequest request);
        Task<bool> DeleteAsync(int id);
    }
}