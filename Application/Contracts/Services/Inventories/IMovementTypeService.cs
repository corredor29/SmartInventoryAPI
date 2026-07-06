using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Inventories.MovementType;

namespace Application.Contracts.Services.Inventories
{
    public interface IMovementTypeService
    {
        Task<IReadOnlyList<MovementTypeDto>> GetAllAsync();
        Task<MovementTypeDto?> GetByIdAsync(int id);
        Task<MovementTypeDto> CreateAsync(CreateMovementTypeRequest request);
        Task<MovementTypeDto?> UpdateAsync(int id, UpdateMovementTypeRequest request);
        Task<bool> DeleteAsync(int id);
    }
}