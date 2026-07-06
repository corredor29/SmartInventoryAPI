using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Chats.SenderType;

namespace Application.Contracts.Services.Chats
{
    public interface ISenderTypeService
    {
        Task<IReadOnlyList<SenderTypeDto>> GetAllAsync();
        Task<SenderTypeDto?> GetByIdAsync(int id);
        Task<SenderTypeDto> CreateAsync(CreateSenderTypeRequest request);
        Task<SenderTypeDto?> UpdateAsync(int id, UpdateSenderTypeRequest request);
        Task<bool> DeleteAsync(int id);
    }
}