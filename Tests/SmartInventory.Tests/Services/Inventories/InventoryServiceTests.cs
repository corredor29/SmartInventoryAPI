using Application.Contracts.Repositories;
using Application.DTOs.Inventories.Inventory;
using Application.Services.Inventories;
using Domain.Entities.Inventories;
using Domain.ValueObject.Inventories.Inventory;
using Moq;
using SmartInventory.Tests.TestHelpers;

namespace SmartInventory.Tests.Services.Inventories
{
    public class InventoryServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IInventoryRepository> _inventoryRepositoryMock = new();
        private readonly Mock<IRepository<InventoryMovement>> _inventoryMovementRepositoryMock = new();
        private readonly InventoryService _sut;

        public InventoryServiceTests()
        {
            _unitOfWorkMock.SetupGet(u => u.Inventory).Returns(_inventoryRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<InventoryMovement>()).Returns(_inventoryMovementRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _sut = new InventoryService(_unitOfWorkMock.Object);
        }

        private static Inventory BuildInventory(int inventoryId, int productId, int stock)
        {
            var inventory = new Inventory(productId, new StockQuantity(stock));
            EntityReflectionHelper.SetId(inventory, inventoryId);
            return inventory;
        }

        [Fact]
        public async Task AdjustStockAsync_PositiveQuantityChange_IncreasesStockAndRecordsMovement()
        {
            // Arrange
            var inventory = BuildInventory(inventoryId: 50, productId: 1, stock: 10);
            _inventoryRepositoryMock.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync(inventory);
            _inventoryMovementRepositoryMock.Setup(r => r.AddAsync(It.IsAny<InventoryMovement>())).Returns(Task.CompletedTask);

            var request = new UpdateInventoryRequest { QuantityChange = 5, Reason = "Reposición de stock" };

            // Act
            var result = await _sut.AdjustStockAsync(1, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result!.CurrentStock);
            _inventoryRepositoryMock.Verify(r => r.Update(It.Is<Inventory>(i => i.CurrentStock.Value == 15)), Times.Once);
            _inventoryMovementRepositoryMock.Verify(
                r => r.AddAsync(It.Is<InventoryMovement>(m => m.Quantity.Value == 5 && m.MovementTypeId == 1)),
                Times.Once);
        }

        [Fact]
        public async Task AdjustStockAsync_NegativeQuantityChange_DecreasesStockAndRecordsMovement()
        {
            // Arrange
            var inventory = BuildInventory(inventoryId: 50, productId: 1, stock: 10);
            _inventoryRepositoryMock.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync(inventory);
            _inventoryMovementRepositoryMock.Setup(r => r.AddAsync(It.IsAny<InventoryMovement>())).Returns(Task.CompletedTask);

            var request = new UpdateInventoryRequest { QuantityChange = -4, Reason = "Venta manual" };

            // Act
            var result = await _sut.AdjustStockAsync(1, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result!.CurrentStock);
            _inventoryRepositoryMock.Verify(r => r.Update(It.Is<Inventory>(i => i.CurrentStock.Value == 6)), Times.Once);
            _inventoryMovementRepositoryMock.Verify(
                r => r.AddAsync(It.Is<InventoryMovement>(m => m.Quantity.Value == 4 && m.MovementTypeId == 2)),
                Times.Once);
        }

        [Fact]
        public async Task AdjustStockAsync_QuantityExceedsCurrentStock_ThrowsInvalidOperationException()
        {
            // Arrange
            var inventory = BuildInventory(inventoryId: 50, productId: 1, stock: 3);
            _inventoryRepositoryMock.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync(inventory);

            var request = new UpdateInventoryRequest { QuantityChange = -10, Reason = "Ajuste inválido" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AdjustStockAsync(1, request));

            _inventoryRepositoryMock.Verify(r => r.Update(It.IsAny<Inventory>()), Times.Never);
            _inventoryMovementRepositoryMock.Verify(r => r.AddAsync(It.IsAny<InventoryMovement>()), Times.Never);
        }

        [Fact]
        public async Task AdjustStockAsync_ProductWithoutInventory_ReturnsNull()
        {
            // Arrange
            _inventoryRepositoryMock.Setup(r => r.GetByProductIdAsync(99)).ReturnsAsync((Inventory?)null);

            // Act
            var result = await _sut.AdjustStockAsync(99, new UpdateInventoryRequest { QuantityChange = 5 });

            // Assert
            Assert.Null(result);
            _inventoryRepositoryMock.Verify(r => r.Update(It.IsAny<Inventory>()), Times.Never);
        }
    }
}
