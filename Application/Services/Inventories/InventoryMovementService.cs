using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Inventories;
using Application.DTOs.Inventories.InventoryMovement;
using Domain.Entities.Inventories;

namespace Application.Services.Inventories
{
    public class InventoryMovementService : IInventoryMovementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryMovementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<InventoryMovementDto>> GetAllAsync()
        {
            var movements = await _unitOfWork.InventoryMovements.GetAllAsync();
            return movements.Select(ToDto).ToList();
        }

        public async Task<InventoryMovementDto?> GetByIdAsync(int id)
        {
            var movement = await _unitOfWork.InventoryMovements.GetByIdAsync(id);
            return movement is null ? null : ToDto(movement);
        }

        public async Task<IReadOnlyList<InventoryMovementDto>> GetByInventoryIdAsync(int inventoryId)
        {
            var movements = await _unitOfWork.InventoryMovements.GetByInventoryIdAsync(inventoryId);
            return movements.Select(ToDto).ToList();
        }

        private static InventoryMovementDto ToDto(InventoryMovement movement) => new()
        {
            MovementId = movement.Id,
            InventoryId = movement.InventoryId,
            ProductName = movement.Inventory?.Product?.Name.Value ?? string.Empty,
            MovementTypeName = movement.MovementType?.Name.Value ?? string.Empty,
            Quantity = movement.Quantity.Value,
            Reason = movement.Reason?.Value,
            CreatedAt = movement.CreatedAt,
        };
    }
}
