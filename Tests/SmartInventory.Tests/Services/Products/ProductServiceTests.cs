using Application.Contracts.Repositories;
using Application.Contracts.Services;
using Application.DTOs.Products.Product;
using Application.Services.Products;
using Domain.Entities.Inventories;
using Domain.Entities.Products;
using Domain.ValueObject.Products.Product;
using Moq;
using SmartInventory.Tests.TestHelpers;

namespace SmartInventory.Tests.Services.Products
{
    public class ProductServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IProductRepository> _productRepositoryMock = new();
        private readonly Mock<IInventoryRepository> _inventoryRepositoryMock = new();
        private readonly Mock<IEmbeddingService> _embeddingMock = new();
        private readonly ProductService _sut;

        public ProductServiceTests()
        {
            _unitOfWorkMock.SetupGet(u => u.Products).Returns(_productRepositoryMock.Object);
            _unitOfWorkMock.SetupGet(u => u.Inventory).Returns(_inventoryRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _embeddingMock.SetupGet(e => e.IsConfigured).Returns(false);

            _sut = new ProductService(_unitOfWorkMock.Object, _embeddingMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_CreatesProductAndZeroInventory()
        {
            // Arrange
            Product? created = null;
            _productRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Callback<Product>(p =>
                {
                    EntityReflectionHelper.SetId(p, 42);
                    created = p;
                })
                .Returns(Task.CompletedTask);
            _inventoryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Inventory>())).Returns(Task.CompletedTask);
            _productRepositoryMock
                .Setup(r => r.GetByIdWithInventoryAsync(42))
                .ReturnsAsync(() => created);

            var request = new CreateProductRequest
            {
                Name = "Mouse inalámbrico",
                Description = "Mouse ergonómico",
                Price = 99.99m,
                CategoryId = 1,
                ProductStatusId = 1,
                InitialStock = 0,
            };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.Equal("Mouse inalámbrico", result.Name);
            Assert.Equal(99.99m, result.Price);
            Assert.Equal(0, result.CurrentStock);

            _productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
            _inventoryRepositoryMock.Verify(
                r => r.AddAsync(It.Is<Inventory>(i => i.ProductId == 42 && i.CurrentStock.Value == 0)),
                Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task CreateAsync_WithInitialStock_CreatesInventoryAndEntradaMovement()
        {
            // Arrange
            Product? created = null;
            Inventory? createdInventory = null;

            _productRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Callback<Product>(p =>
                {
                    EntityReflectionHelper.SetId(p, 42);
                    created = p;
                })
                .Returns(Task.CompletedTask);

            _inventoryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Inventory>()))
                .Callback<Inventory>(i =>
                {
                    EntityReflectionHelper.SetId(i, 77);
                    createdInventory = i;
                })
                .Returns(Task.CompletedTask);

            var entrada = new MovementType(Domain.ValueObject.Products.MovementType.MovementTypeName.Create("Entrada"));
            EntityReflectionHelper.SetId(entrada, 1);
            var movementTypeRepo = new Mock<IRepository<MovementType>>();
            movementTypeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<MovementType> { entrada });
            _unitOfWorkMock.Setup(u => u.Repository<MovementType>()).Returns(movementTypeRepo.Object);

            var movementRepo = new Mock<IRepository<InventoryMovement>>();
            movementRepo.Setup(r => r.AddAsync(It.IsAny<InventoryMovement>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.Repository<InventoryMovement>()).Returns(movementRepo.Object);

            _productRepositoryMock
                .Setup(r => r.GetByIdWithInventoryAsync(42))
                .ReturnsAsync(() =>
                {
                    if (created is null) return null!;
                    EntityReflectionHelper.SetProperty(created, nameof(Product.Inventory), createdInventory!);
                    return created;
                });

            var request = new CreateProductRequest
            {
                Name = "Monitor 27",
                Price = 800m,
                CategoryId = 1,
                ProductStatusId = 1,
                InitialStock = 15,
            };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.Equal(15, result.CurrentStock);
            _inventoryRepositoryMock.Verify(
                r => r.AddAsync(It.Is<Inventory>(i => i.ProductId == 42 && i.CurrentStock.Value == 15)),
                Times.Once);
            movementRepo.Verify(
                r => r.AddAsync(It.Is<InventoryMovement>(m =>
                    m.InventoryId == 77 &&
                    m.MovementTypeId == 1 &&
                    m.Quantity.Value == 15)),
                Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DelegatesToRepositoryAndMapsResults()
        {
            // Arrange
            var product1 = new Product(1, 1, new ProductName("Teclado"), new ProductPrice(150m));
            EntityReflectionHelper.SetId(product1, 1);
            var product2 = new Product(1, 1, new ProductName("Teclado mecánico"), new ProductPrice(250m));
            EntityReflectionHelper.SetId(product2, 2);

            _productRepositoryMock.Setup(r => r.SearchAsync("teclado")).ReturnsAsync(new List<Product> { product1, product2 });

            // Act
            var result = await _sut.SearchAsync("teclado");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Teclado", result[0].Name);
            Assert.Equal("Teclado mecánico", result[1].Name);
            _productRepositoryMock.Verify(r => r.SearchAsync("teclado"), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusAsync_ProductExists_UpdatesStatusAndReturnsDto()
        {
            // Arrange
            var product = new Product(1, 1, new ProductName("Monitor"), new ProductPrice(500m));
            EntityReflectionHelper.SetId(product, 7);

            _productRepositoryMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(product);
            _productRepositoryMock.Setup(r => r.GetByIdWithInventoryAsync(7)).ReturnsAsync(product);

            // Act
            var result = await _sut.ChangeStatusAsync(7, 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Monitor", result!.Name);
            _productRepositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusAsync_ProductNotFound_ReturnsNull()
        {
            // Arrange
            _productRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

            // Act
            var result = await _sut.ChangeStatusAsync(999, 2);

            // Assert
            Assert.Null(result);
            _productRepositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
