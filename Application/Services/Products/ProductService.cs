using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services;
using Application.Contracts.Services.Products;
using Application.DTOs.Products.Product;
using Domain.Entities.Inventories;
using Domain.Entities.Products;
using Domain.ValueObject.Inventories.Inventory;
using Domain.ValueObject.Products.Product;
using Pgvector;

namespace Application.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embeddingService;

        public ProductService(IUnitOfWork unitOfWork, IEmbeddingService embeddingService)
        {
            _unitOfWork = unitOfWork;
            _embeddingService = embeddingService;
        }

        public async Task<IReadOnlyList<ProductDto>> GetAllAsync()
        {
            var products = await _unitOfWork.Products.GetAllWithDetailsAsync();
            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            return product is null ? null : ToDto(product);
        }

        public async Task<IReadOnlyList<ProductDto>> SearchAsync(string query)
        {
            // 1) Busqueda semantica con pgvector si hay embeddings + API key
            if (_embeddingService.IsConfigured)
            {
                var embedding = await _embeddingService.CreateEmbeddingAsync(query);
                if (embedding is not null)
                {
                    var semantic = await _unitOfWork.Products.SearchByEmbeddingAsync(embedding);
                    if (semantic.Count > 0)
                        return semantic.Select(ToDto).ToList();
                }
            }

            // 2) Fallback: busqueda por tokens de texto
            var products = await _unitOfWork.Products.SearchAsync(query);
            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto> CreateAsync(CreateProductRequest request)
        {
            if (request.InitialStock < 0)
                throw new ArgumentException("El stock inicial no puede ser negativo.");

            var product = new Product(
                categoryId: request.CategoryId,
                productStatusId: request.ProductStatusId,
                name: new ProductName(request.Name),
                price: new ProductPrice(request.Price),
                description: request.Description is null ? null : new ProductDescription(request.Description),
                imageUrl: string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
            );

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            var initialStock = request.InitialStock > 0
                ? new StockQuantity(request.InitialStock)
                : StockQuantity.Zero;

            var inventory = new Inventory(product.Id, initialStock);
            await _unitOfWork.Inventory.AddAsync(inventory);
            await _unitOfWork.SaveChangesAsync();

            if (request.InitialStock > 0)
            {
                var movementTypeId = await ResolveMovementTypeIdAsync("Entrada");
                var movement = InventoryMovement.CreateExit(
                    inventoryId: inventory.Id,
                    movementTypeId: movementTypeId,
                    quantity: request.InitialStock,
                    reason: "Stock inicial al crear producto"
                );
                await _unitOfWork.Repository<InventoryMovement>().AddAsync(movement);
                await _unitOfWork.SaveChangesAsync();
            }

            var created = await _unitOfWork.Products.GetByIdWithInventoryAsync(product.Id)
                ?? product;

            await TrySetEmbeddingAsync(created);

            var reloaded = await _unitOfWork.Products.GetByIdWithInventoryAsync(product.Id)
                ?? created;
            return ToDto(reloaded);
        }

        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return null;

            product.Update(
                name: new ProductName(request.Name),
                price: new ProductPrice(request.Price),
                description: request.Description is null ? null : new ProductDescription(request.Description),
                categoryId: request.CategoryId,
                imageUrl: string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
            );

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            if (updated is not null)
                await TrySetEmbeddingAsync(updated);

            var reloaded = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            return reloaded is null ? null : ToDto(reloaded);
        }

        public async Task<ProductDto?> ChangeStatusAsync(int id, int productStatusId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return null;

            product.ChangeStatus(productStatusId);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            return updated is null ? null : ToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return false;

            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<int> ReindexEmbeddingsAsync()
        {
            if (!_embeddingService.IsConfigured)
                throw new InvalidOperationException(
                    "OpenAI:ApiKey no configurada. Define OpenAI:ApiKey o la variable OPENAI_API_KEY.");

            var products = await _unitOfWork.Products.GetAllWithDetailsAsync();
            var updated = 0;

            foreach (var product in products)
            {
                if (await TrySetEmbeddingAsync(product))
                    updated++;
            }

            return updated;
        }

        private async Task<bool> TrySetEmbeddingAsync(Product product)
        {
            if (!_embeddingService.IsConfigured)
                return false;

            var text = BuildEmbeddingText(product);
            var values = await _embeddingService.CreateEmbeddingAsync(text);
            if (values is null || values.Length == 0)
                return false;

            product.SetEmbedding(new Vector(values));
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static string BuildEmbeddingText(Product product)
        {
            var category = product.Category?.Name.Value ?? string.Empty;
            var description = product.Description?.Value ?? string.Empty;
            return $"{product.Name.Value}. {description}. Categoria: {category}".Trim();
        }

        private async Task<int> ResolveMovementTypeIdAsync(string name)
        {
            var items = await _unitOfWork.Repository<MovementType>().GetAllAsync();
            var match = items.FirstOrDefault(i =>
                string.Equals(i.Name.Value, name, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                throw new InvalidOperationException($"No se encontro el tipo de movimiento '{name}'.");

            return match.Id;
        }

        private static ProductDto ToDto(Product product) => new()
        {
            ProductId = product.Id,
            Name = product.Name.Value,
            Description = product.Description?.Value,
            Price = product.Price.Value,
            CategoryName = product.Category?.Name.Value ?? string.Empty,
            StatusName = product.ProductStatus?.Name.Value ?? string.Empty,
            CurrentStock = product.Inventory?.CurrentStock.Value ?? 0,
            ImageUrl = product.ImageUrl,
        };
    }
}
