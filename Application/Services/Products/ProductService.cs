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
    /// <summary>
    /// Servicio de productos que contiene la lógica de negocio para la gestión del catálogo.
    /// Maneja CRUD de productos, búsqueda semántica con embeddings y gestión de inventario inicial.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embeddingService;

        /// <summary>
        /// Constructor que inyecta las dependencias necesarias mediante inyección de dependencias.
        /// </summary>
        /// <param name="unitOfWork">Unit of Work para acceso a datos y transacciones.</param>
        /// <param name="embeddingService">Servicio para generar embeddings vectoriales con OpenAI.</param>
        public ProductService(IUnitOfWork unitOfWork, IEmbeddingService embeddingService)
        {
            _unitOfWork = unitOfWork;
            _embeddingService = embeddingService;
        }

        /// <summary>
        /// Obtiene todos los productos del catálogo con sus detalles (categoría, estado, inventario).
        /// </summary>
        /// <returns>Lista de DTOs con la información de todos los productos.</returns>
        public async Task<IReadOnlyList<ProductDto>> GetAllAsync()
        {
            // Obtiene todos los productos incluyendo sus relaciones (categoría, estado, inventario)
            var products = await _unitOfWork.Products.GetAllWithDetailsAsync();
            // Convierte cada entidad Product a ProductDto usando el método ToDto
            return products.Select(ToDto).ToList();
        }

        /// <summary>
        /// Obtiene un producto específico por su ID incluyendo su inventario.
        /// </summary>
        /// <param name="id">ID del producto a buscar.</param>
        /// <returns>DTO con los detalles del producto si existe, null si no existe.</returns>
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            // Obtiene el producto por ID incluyendo su inventario
            var product = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            return product is null ? null : ToDto(product);
        }

        /// <summary>
        /// Busca productos usando búsqueda semántica con pgvector o fallback a búsqueda por texto.
        /// </summary>
        /// <param name="query">Texto de búsqueda (query del usuario).</param>
        /// <returns>Lista de DTOs con los productos encontrados.</returns>
        /// <remarks>
        /// Estrategia de búsqueda en dos niveles:
        /// 1. Si está configurada la API key de OpenAI, genera un embedding del query
        ///    y busca productos similares usando pgvector (búsqueda semántica).
        /// 2. Si no hay API key o la búsqueda semántica no devuelve resultados,
        ///    hace fallback a búsqueda por tokens de texto (LIKE SQL).
        /// </remarks>
        public async Task<IReadOnlyList<ProductDto>> SearchAsync(string query)
        {
            // ==============================================================================
            // ESTRATEGIA 1: BÚSQUEDA SEMÁNTICA CON PGVECTOR
            // ==============================================================================
            // Si está configurado el servicio de embeddings (hay API key de OpenAI),
            // intenta búsqueda semántica que es más precisa y entiende el contexto.
            if (_embeddingService.IsConfigured)
            {
                // Genera un embedding vectorial del query usando OpenAI
                var embedding = await _embeddingService.CreateEmbeddingAsync(query);
                if (embedding is not null)
                {
                    // Busca productos similares usando pgvector (búsqueda de similitud coseno)
                    var semantic = await _unitOfWork.Products.SearchByEmbeddingAsync(embedding);
                    if (semantic.Count > 0)
                        return semantic.Select(ToDto).ToList();
                }
            }

            // ==============================================================================
            // ESTRATEGIA 2: FALLBACK A BÚSQUEDA POR TEXTO
            // ==============================================================================
            // Si no hay API key o la búsqueda semántica no dio resultados,
            // usa búsqueda por tokens de texto (LIKE SQL) como fallback.
            var products = await _unitOfWork.Products.SearchAsync(query);
            return products.Select(ToDto).ToList();
        }

        /// <summary>
        /// Crea un nuevo producto con su inventario inicial.
        /// Si hay stock inicial > 0, registra un movimiento de entrada.
        /// Si hay API key de OpenAI, genera el embedding automáticamente.
        /// </summary>
        /// <param name="request">DTO con los datos del producto a crear.</param>
        /// <returns>DTO con el producto creado incluyendo su inventario.</returns>
        /// <exception cref="ArgumentException">Se lanza si el stock inicial es negativo.</exception>
        public async Task<ProductDto> CreateAsync(CreateProductRequest request)
        {
            // Valida que el stock inicial no sea negativo
            if (request.InitialStock < 0)
                throw new ArgumentException("El stock inicial no puede ser negativo.");

            // ==============================================================================
            // CREACIÓN DEL PRODUCTO
            // ==============================================================================
            // Crea la entidad Product usando Value Objects para validación de dominio
            var product = new Product(
                categoryId: request.CategoryId,
                productStatusId: request.ProductStatusId,
                name: new ProductName(request.Name),
                price: new ProductPrice(request.Price),
                description: request.Description is null ? null : new ProductDescription(request.Description),
                imageUrl: string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
            );

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync(); // Guarda para obtener el ID del producto

            // ==============================================================================
            // CREACIÓN DEL INVENTARIO
            // ==============================================================================
            // Crea el inventario asociado al producto con el stock inicial
            var initialStock = request.InitialStock > 0
                ? new StockQuantity(request.InitialStock)
                : StockQuantity.Zero;

            var inventory = new Inventory(product.Id, initialStock);
            await _unitOfWork.Inventory.AddAsync(inventory);
            await _unitOfWork.SaveChangesAsync();

            // ==============================================================================
            // REGISTRO DE MOVIMIENTO DE ENTRADA (SI HAY STOCK INICIAL)
            // ==============================================================================
            // Si el stock inicial es mayor a 0, registra un movimiento de entrada
            // para mantener el historial de cambios de inventario.
            if (request.InitialStock > 0)
            {
                // Resuelve el ID del tipo de movimiento "Entrada"
                var movementTypeId = await ResolveMovementTypeIdAsync("Entrada");
                // Crea el movimiento de inventario (nota: el método se llama CreateExit pero es genérico)
                var movement = InventoryMovement.CreateExit(
                    inventoryId: inventory.Id,
                    movementTypeId: movementTypeId,
                    quantity: request.InitialStock,
                    reason: "Stock inicial al crear producto"
                );
                await _unitOfWork.Repository<InventoryMovement>().AddAsync(movement);
                await _unitOfWork.SaveChangesAsync();
            }

            // Recarga el producto con sus relaciones (inventario, categoría, estado)
            var created = await _unitOfWork.Products.GetByIdWithInventoryAsync(product.Id)
                ?? product;

            // ==============================================================================
            // GENERACIÓN DE EMBEDDING (SI HAY API KEY)
            // ==============================================================================
            // Si está configurada la API key de OpenAI, genera el embedding vectorial
            // para habilitar búsqueda semántica del producto.
            await TrySetEmbeddingAsync(created);

            // Recarga nuevamente para asegurar que tenemos el embedding actualizado
            var reloaded = await _unitOfWork.Products.GetByIdWithInventoryAsync(product.Id)
                ?? created;
            return ToDto(reloaded);
        }

        /// <summary>
        /// Actualiza un producto existente.
        /// Si se modificó nombre/descripción/categoría, regenera el embedding automáticamente.
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="request">DTO con los datos actualizados del producto.</param>
        /// <returns>DTO con el producto actualizado si existe, null si no existe.</returns>
        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request)
        {
            // Busca el producto por ID
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return null;

            // Actualiza los datos del producto usando el método de dominio Update
            product.Update(
                name: new ProductName(request.Name),
                price: new ProductPrice(request.Price),
                description: request.Description is null ? null : new ProductDescription(request.Description),
                categoryId: request.CategoryId,
                imageUrl: string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
            );

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // Recarga el producto con sus relaciones
            var updated = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            if (updated is not null)
                // Regenera el embedding si se modificaron campos relevantes
                await TrySetEmbeddingAsync(updated);

            // Recarga nuevamente para asegurar que tenemos el embedding actualizado
            var reloaded = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            return reloaded is null ? null : ToDto(reloaded);
        }

        /// <summary>
        /// Cambia el estado de un producto (Activo/Inactivo).
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="productStatusId">ID del nuevo estado de producto.</param>
        /// <returns>DTO con el producto actualizado si existe, null si no existe.</returns>
        public async Task<ProductDto?> ChangeStatusAsync(int id, int productStatusId)
        {
            // Busca el producto por ID
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return null;

            // Cambia el estado usando el método de dominio ChangeStatus
            product.ChangeStatus(productStatusId);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // Recarga el producto con sus relaciones
            var updated = await _unitOfWork.Products.GetByIdWithInventoryAsync(id);
            return updated is null ? null : ToDto(updated);
        }

        /// <summary>
        /// Elimina un producto del catálogo.
        /// </summary>
        /// <param name="id">ID del producto a eliminar.</param>
        /// <returns>True si el producto fue eliminado, false si no existe.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            // Busca el producto por ID
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return false;

            // Elimina el producto (EF Core maneja la eliminación en cascada si está configurado)
            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Regenera los embeddings vectoriales de todos los productos.
        /// Útil cuando se agrega la API key de OpenAI después de crear productos,
        /// o cuando se actualizan descripciones masivamente.
        /// </summary>
        /// <returns>Cantidad de productos actualizados con embeddings.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no está configurada la API key de OpenAI.
        /// </exception>
        public async Task<int> ReindexEmbeddingsAsync()
        {
            // Valida que esté configurada la API key de OpenAI
            if (!_embeddingService.IsConfigured)
                throw new InvalidOperationException(
                    "OpenAI:ApiKey no configurada. Define OpenAI:ApiKey o la variable OPENAI_API_KEY.");

            // Obtiene todos los productos
            var products = await _unitOfWork.Products.GetAllWithDetailsAsync();
            var updated = 0;

            // Recorre todos los productos y regenera sus embeddings
            foreach (var product in products)
            {
                if (await TrySetEmbeddingAsync(product))
                    updated++;
            }

            return updated;
        }

        /// <summary>
        /// Método privado que intenta generar y asignar un embedding a un producto.
        /// </summary>
        /// <param name="product">Producto al que se le asignará el embedding.</param>
        /// <returns>True si el embedding fue generado y asignado, false si no.</returns>
        private async Task<bool> TrySetEmbeddingAsync(Product product)
        {
            // Si no está configurada la API key de OpenAI, no hace nada
            if (!_embeddingService.IsConfigured)
                return false;

            // Construye el texto para generar el embedding (nombre + descripción + categoría)
            var text = BuildEmbeddingText(product);
            // Genera el embedding vectorial usando OpenAI
            var values = await _embeddingService.CreateEmbeddingAsync(text);
            if (values is null || values.Length == 0)
                return false;

            // Asigna el embedding al producto usando el tipo Vector de pgvector
            product.SetEmbedding(new Vector(values));
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Construye el texto para generar el embedding de un producto.
        /// Combina nombre, descripción y categoría para maximizar la relevancia semántica.
        /// </summary>
        /// <param name="product">Producto del cual se construirá el texto.</param>
        /// <returns>Texto concatenado para generar el embedding.</returns>
        private static string BuildEmbeddingText(Product product)
        {
            var category = product.Category?.Name.Value ?? string.Empty;
            var description = product.Description?.Value ?? string.Empty;
            return $"{product.Name.Value}. {description}. Categoria: {category}".Trim();
        }

        /// <summary>
        /// Método privado que resuelve el ID de un tipo de movimiento por su nombre.
        /// </summary>
        /// <param name="name">Nombre del tipo de movimiento (ej: "Entrada", "Salida").</param>
        /// <returns>ID del tipo de movimiento encontrado.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no existe el tipo de movimiento con el nombre especificado.
        /// </exception>
        private async Task<int> ResolveMovementTypeIdAsync(string name)
        {
            // Obtiene todos los tipos de movimiento
            var items = await _unitOfWork.Repository<MovementType>().GetAllAsync();
            // Busca el tipo de movimiento por nombre (case-insensitive)
            var match = items.FirstOrDefault(i =>
                string.Equals(i.Name.Value, name, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                throw new InvalidOperationException($"No se encontro el tipo de movimiento '{name}'.");

            return match.Id;
        }

        /// <summary>
        /// Método estático que convierte una entidad Product a ProductDto.
        /// Mapea las propiedades de la entidad a las propiedades del DTO.
        /// </summary>
        /// <param name="product">Entidad Product a convertir.</param>
        /// <returns>DTO ProductDto con los datos del producto.</returns>
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
