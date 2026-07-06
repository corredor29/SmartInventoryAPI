using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Inventories;
using Application.DTOs.Inventories.Inventory;
using Domain.Entities.Inventories;

namespace Application.Services.Inventories
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<InventoryDto>> GetAllAsync()
        {
            var inventories = await _unitOfWork.Inventory.GetAllAsync();
            return inventories.Select(ToDto).ToList();
        }

        public async Task<InventoryDto?> GetByIdAsync(int id)
        {
            var inventory = await _unitOfWork.Inventory.GetByIdAsync(id);
            return inventory is null ? null : ToDto(inventory);
        }

        public async Task<InventoryDto?> GetByProductIdAsync(int productId)
        {
            var inventory = await _unitOfWork.Inventory.GetByProductIdAsync(productId);
            return inventory is null ? null : ToDto(inventory);
        }

        public async Task<InventoryDto?> AdjustStockAsync(int productId, UpdateInventoryRequest request)
        {
            var inventory = await _unitOfWork.Inventory.GetByProductIdAsync(productId);
            if (inventory is null) return null;

            if (request.QuantityChange > 0)
                inventory.IncreaseStock(request.QuantityChange);
            else if (request.QuantityChange < 0)
                inventory.DecreaseStock(-request.QuantityChange);

            _unitOfWork.Inventory.Update(inventory);

            var movementTypeId = request.QuantityChange > 0 ? 1 : 2; // 1 = Entrada, 2 = Salida (según seed)
            var movement = InventoryMovement.CreateExit(
                inventoryId: inventory.Id,
                movementTypeId: movementTypeId,
                quantity: System.Math.Abs(request.QuantityChange),
                reason: request.Reason
            );

            await _unitOfWork.Repository<InventoryMovement>().AddAsync(movement);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(inventory);
        }

        private static InventoryDto ToDto(Inventory inventory) => new()
        {
            InventoryId = inventory.Id,
            ProductId = inventory.ProductId,
            ProductName = inventory.Product?.Name.Value ?? string.Empty,
            CurrentStock = inventory.CurrentStock.Value,
        };
    }
}