using Application.Contracts.Repositories;
using Application.DTOs.Sales.Sale;
using Application.Services.Sales;
using Domain.Entities.Inventories;
using Domain.Entities.Invoices;
using Domain.Entities.Products;
using Domain.Entities.Sales;
using Domain.ValueObject.Inventories.Inventory;
using Domain.ValueObject.Products.MovementType;
using Domain.ValueObject.Products.Product;
using Domain.ValueObject.Sales.SaleDetail;
using Domain.ValueObject.Sales.SaleOrigin;
using Domain.ValueObject.Sales.SaleStatus;
using Moq;
using SmartInventory.Tests.TestHelpers;

namespace SmartInventory.Tests.Services.Sales
{
    public class SaleServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IProductRepository> _productRepositoryMock = new();
        private readonly Mock<IInventoryRepository> _inventoryRepositoryMock = new();
        private readonly Mock<ISaleRepository> _saleRepositoryMock = new();
        private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock = new();
        private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
        private readonly Mock<IRepository<SaleOrigin>> _saleOriginRepositoryMock = new();
        private readonly Mock<IRepository<SaleStatus>> _saleStatusRepositoryMock = new();
        private readonly Mock<IRepository<MovementType>> _movementTypeRepositoryMock = new();
        private readonly Mock<IRepository<SaleDetail>> _saleDetailRepositoryMock = new();
        private readonly Mock<IRepository<InventoryMovement>> _inventoryMovementRepositoryMock = new();
        private readonly SaleService _sut;

        public SaleServiceTests()
        {
            _unitOfWorkMock.SetupGet(u => u.Products).Returns(_productRepositoryMock.Object);
            _unitOfWorkMock.SetupGet(u => u.Inventory).Returns(_inventoryRepositoryMock.Object);
            _unitOfWorkMock.SetupGet(u => u.Sales).Returns(_saleRepositoryMock.Object);
            _unitOfWorkMock.SetupGet(u => u.Invoices).Returns(_invoiceRepositoryMock.Object);
            _unitOfWorkMock.SetupGet(u => u.Customers).Returns(_customerRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<SaleOrigin>()).Returns(_saleOriginRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<SaleStatus>()).Returns(_saleStatusRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<MovementType>()).Returns(_movementTypeRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<SaleDetail>()).Returns(_saleDetailRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<InventoryMovement>()).Returns(_inventoryMovementRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _sut = new SaleService(_unitOfWorkMock.Object);
        }

        private void SetupLookups(int saleOriginId = 1, int saleStatusId = 2, int movementTypeId = 3)
        {
            var origin = new SaleOrigin(SaleOriginName.Create("Manual"));
            EntityReflectionHelper.SetId(origin, saleOriginId);
            _saleOriginRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SaleOrigin> { origin });

            var status = new SaleStatus(SaleStatusName.Create("Completada"));
            EntityReflectionHelper.SetId(status, saleStatusId);
            _saleStatusRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SaleStatus> { status });

            var movementType = new MovementType(MovementTypeName.Create("Salida"));
            EntityReflectionHelper.SetId(movementType, movementTypeId);
            _movementTypeRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<MovementType> { movementType });
        }

        private static Product BuildProductWithInventory(int productId, int inventoryId, int stock, decimal price = 50m)
        {
            var product = new Product(
                categoryId: 1,
                productStatusId: 1,
                name: new ProductName("Producto Test"),
                price: new ProductPrice(price));
            EntityReflectionHelper.SetId(product, productId);

            var inventory = new Inventory(productId, new StockQuantity(stock));
            EntityReflectionHelper.SetId(inventory, inventoryId);
            EntityReflectionHelper.SetProperty(product, nameof(Product.Inventory), inventory);

            return product;
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_RegistersSaleAndCommits()
        {
            // Arrange
            SetupLookups(saleOriginId: 1, saleStatusId: 2, movementTypeId: 3);

            var product = BuildProductWithInventory(productId: 10, inventoryId: 20, stock: 5, price: 50m);
            _productRepositoryMock.Setup(r => r.GetByIdWithInventoryAsync(10)).ReturnsAsync(product);

            _saleRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Sale>()))
                .Callback<Sale>(s => EntityReflectionHelper.SetId(s, 100))
                .Returns(Task.CompletedTask);

            _saleDetailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SaleDetail>())).Returns(Task.CompletedTask);
            _inventoryMovementRepositoryMock.Setup(r => r.AddAsync(It.IsAny<InventoryMovement>())).Returns(Task.CompletedTask);
            _invoiceRepositoryMock.Setup(r => r.GetNextSequenceAsync()).ReturnsAsync(1);
            _invoiceRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

            var fullSale = new Sale(customerId: 5, saleOriginId: 1, saleStatusId: 2, paymentMethod: Domain.ValueObject.Sales.Sale.PaymentMethod.Efectivo);
            EntityReflectionHelper.SetId(fullSale, 100);
            fullSale.AddDetail(new SaleDetail(100, 10, new SaleDetailQuantity(2), new UnitPrice(50m)));
            _saleRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(100)).ReturnsAsync(fullSale);

            var request = new CreateSaleRequest
            {
                CustomerId = 5,
                Origin = "Manual",
                PaymentMethod = "Efectivo",
                DeliveryAddress = "Calle 1 #2-3",
                ContactPhone = "3001234567",
                ContactDocument = "1234567890",
                Items = new List<SaleItemRequest> { new() { ProductId = 10, Quantity = 2 } },
            };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(100, result.SaleId);
            Assert.Equal("FAC-000001", result.InvoiceNumber);
            Assert.Equal(100m, result.Total);

            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            _inventoryRepositoryMock.Verify(r => r.Update(It.Is<Inventory>(i => i.CurrentStock.Value == 3)), Times.Once);
            _inventoryMovementRepositoryMock.Verify(r => r.AddAsync(It.IsAny<InventoryMovement>()), Times.Once);
            _saleDetailRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SaleDetail>()), Times.Once);
            _invoiceRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ProductDoesNotExist_ReturnsFailureAndRollsBack()
        {
            // Arrange
            SetupLookups();
            _productRepositoryMock.Setup(r => r.GetByIdWithInventoryAsync(99)).ReturnsAsync((Product?)null);

            var request = new CreateSaleRequest
            {
                CustomerId = 5,
                Origin = "Manual",
                PaymentMethod = "Efectivo",
                DeliveryAddress = "Calle 1 #2-3",
                ContactPhone = "3001234567",
                ContactDocument = "1234567890",
                Items = new List<SaleItemRequest> { new() { ProductId = 99, Quantity = 1 } },
            };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("no existe", result.Message);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_InsufficientStock_ReturnsFailureAndRollsBack()
        {
            // Arrange
            SetupLookups();
            var product = BuildProductWithInventory(productId: 10, inventoryId: 20, stock: 1);
            _productRepositoryMock.Setup(r => r.GetByIdWithInventoryAsync(10)).ReturnsAsync(product);

            var request = new CreateSaleRequest
            {
                CustomerId = 5,
                Origin = "Manual",
                PaymentMethod = "Tarjeta",
                DeliveryAddress = "Calle 1 #2-3",
                ContactPhone = "3001234567",
                ContactDocument = "1234567890",
                Items = new List<SaleItemRequest> { new() { ProductId = 10, Quantity = 5 } },
            };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Stock insuficiente", result.Message);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_EmptyItemsList_ReturnsFailureWithoutStartingTransaction()
        {
            // Arrange
            var request = new CreateSaleRequest { CustomerId = 5, Items = new List<SaleItemRequest>() };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("La venta debe tener al menos un producto.", result.Message);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_UnexpectedException_ReturnsFailureAndRollsBack()
        {
            // Arrange
            SetupLookups();
            var product = BuildProductWithInventory(productId: 10, inventoryId: 20, stock: 5);
            _productRepositoryMock.Setup(r => r.GetByIdWithInventoryAsync(10)).ReturnsAsync(product);

            _saleRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Sale>()))
                .Callback<Sale>(s => EntityReflectionHelper.SetId(s, 100))
                .Returns(Task.CompletedTask);
            _saleDetailRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SaleDetail>())).Returns(Task.CompletedTask);
            _inventoryMovementRepositoryMock.Setup(r => r.AddAsync(It.IsAny<InventoryMovement>())).Returns(Task.CompletedTask);
            _invoiceRepositoryMock
                .Setup(r => r.GetNextSequenceAsync())
                .ThrowsAsync(new InvalidOperationException("Fallo inesperado de base de datos"));

            var request = new CreateSaleRequest
            {
                CustomerId = 5,
                Origin = "Manual",
                PaymentMethod = "Efectivo",
                DeliveryAddress = "Calle 1 #2-3",
                ContactPhone = "3001234567",
                ContactDocument = "1234567890",
                Items = new List<SaleItemRequest> { new() { ProductId = 10, Quantity = 2 } },
            };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Fallo inesperado de base de datos", result.Message);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangeStatusAsync_SaleExists_UpdatesStatusAndReturnsDto()
        {
            // Arrange
            var sale = new Sale(customerId: 5, saleOriginId: 1, saleStatusId: 1, paymentMethod: Domain.ValueObject.Sales.Sale.PaymentMethod.Tarjeta);
            EntityReflectionHelper.SetId(sale, 1);

            _saleRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(sale);
            _saleRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(sale);

            // Act
            var result = await _sut.ChangeStatusAsync(1, 3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.SaleId);
            _saleRepositoryMock.Verify(r => r.Update(It.IsAny<Sale>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusAsync_SaleNotFound_ReturnsNull()
        {
            // Arrange
            _saleRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Sale?)null);

            // Act
            var result = await _sut.ChangeStatusAsync(999, 3);

            // Assert
            Assert.Null(result);
            _saleRepositoryMock.Verify(r => r.Update(It.IsAny<Sale>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
