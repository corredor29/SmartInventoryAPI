using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Sales;
using Domain.ValueObject.Products.Product;
using Domain.Entities.Inventories;
namespace Domain.Entities.Products
{
    /// <summary>
    /// Entidad que representa un producto en el catálogo de la tienda.
    /// Contiene información de nombre, descripción, precio, categoría, estado,
    /// imagen y embedding vectorial para búsqueda semántica.
    /// </summary>
    /// <remarks>
    /// Esta entidad:
    /// - Usa Value Objects para validación de dominio (ProductName, ProductPrice, ProductDescription)
    /// - Tiene una relación uno-a-uno con Inventory para control de stock
    /// - Tiene una relación muchos-a-muchos con Sales a través de SaleDetail
    /// - Almacena un embedding vectorial (pgvector) para búsqueda semántica con OpenAI
    /// - Es sealed para prevenir herencia y mantener la integridad del dominio
    /// </remarks>
    public sealed class Product : BaseEntity
    {
        // ==============================================================================
        // PROPIEDADES - DATOS DEL PRODUCTO
        // ==============================================================================
        /// <summary>ID de la categoría a la que pertenece el producto (FK).</summary>
        public int                CategoryId      { get; private set; }

        /// <summary>ID del estado del producto (Activo/Inactivo) (FK).</summary>
        public int                ProductStatusId { get; private set; }

        /// <summary>Nombre del producto (Value Object con validación).</summary>
        public ProductName        Name            { get; private set; } = null!;

        /// <summary>Descripción detallada del producto (Value Object opcional).</summary>
        public ProductDescription? Description    { get; private set; }

        /// <summary>Precio del producto (Value Object con validación).</summary>
        public ProductPrice       Price           { get; private set; } = null!;

        /// <summary>Embedding vectorial para búsqueda semántica (pgvector).</summary>
        public Pgvector.Vector?   Embedding       { get; private set; }

        /// <summary>URL de la imagen del producto (opcional).</summary>
        public string?            ImageUrl        { get; private set; }

        // ==============================================================================
        // PROPIEDADES DE NAVEGACIÓN - RELACIONES
        // ==============================================================================
        /// <summary>Categoría a la que pertenece el producto (relación de navegación).</summary>
        public Category       Category      { get; private set; } = null!;

        /// <summary>Estado del producto (relación de navegación).</summary>
        public ProductStatus  ProductStatus { get; private set; } = null!;

        /// <summary>Inventario asociado al producto (relación uno-a-uno).</summary>
        public Inventory?     Inventory     { get; private set; }

        /// <summary>Detalles de venta donde aparece este producto (relación muchos-a-muchos).</summary>
        public ICollection<SaleDetail> SaleDetails { get; private set; } = new List<SaleDetail>();

        // ==============================================================================
        // CONSTRUCTOR PRIVADO PARA EF CORE
        // ==============================================================================
        /// <summary>
        /// Constructor privado requerido por Entity Framework Core.
        /// No debe usarse directamente para creación de productos.
        /// </summary>
        private Product() { }

        // ==============================================================================
        // CONSTRUCTOR PÚBLICO - CREACIÓN DE PRODUCTO
        // ==============================================================================
        /// <summary>
        /// Constructor público para crear un nuevo producto.
        /// Valida todos los parámetros usando Value Objects.
        /// </summary>
        /// <param name="categoryId">ID de la categoría (debe ser mayor a 0).</param>
        /// <param name="productStatusId">ID del estado del producto (debe ser mayor a 0).</param>
        /// <param name="name">Nombre del producto (Value Object).</param>
        /// <param name="price">Precio del producto (Value Object).</param>
        /// <param name="description">Descripción opcional del producto (Value Object).</param>
        /// <param name="imageUrl">URL opcional de la imagen del producto.</param>
        /// <exception cref="ArgumentException">Se lanza si categoryId o productStatusId no son válidos.</exception>
        /// <exception cref="ArgumentNullException">Se lanza si name o price son null.</exception>
        public Product(int categoryId, int productStatusId, ProductName name, ProductPrice price, ProductDescription? description = null, string? imageUrl = null)
        {
            // Valida que los IDs sean positivos
            CategoryId      = categoryId      > 0 ? categoryId      : throw new ArgumentException("CategoryId must be greater than 0.");
            ProductStatusId = productStatusId > 0 ? productStatusId : throw new ArgumentException("ProductStatusId must be greater than 0.");
            // Valida que los Value Objects no sean null
            Name            = name  ?? throw new ArgumentNullException(nameof(name));
            Price           = price ?? throw new ArgumentNullException(nameof(price));
            // Los campos opcionales pueden ser null
            Description     = description;
            ImageUrl        = imageUrl;
        }

        // ==============================================================================
        // MÉTODOS DE DOMINIO - ACTUALIZACIÓN
        // ==============================================================================
        /// <summary>
        /// Actualiza los datos del producto.
        /// Valida los parámetros usando Value Objects.
        /// </summary>
        /// <param name="name">Nuevo nombre del producto (Value Object).</param>
        /// <param name="price">Nuevo precio del producto (Value Object).</param>
        /// <param name="description">Nueva descripción opcional (Value Object).</param>
        /// <param name="categoryId">Nuevo ID de categoría (debe ser mayor a 0).</param>
        /// <param name="imageUrl">Nueva URL de imagen opcional.</param>
        /// <exception cref="ArgumentException">Se lanza si categoryId no es válido.</exception>
        /// <exception cref="ArgumentNullException">Se lanza si name o price son null.</exception>
        public void Update(ProductName name, ProductPrice price, ProductDescription? description, int categoryId, string? imageUrl)
        {
            Name        = name  ?? throw new ArgumentNullException(nameof(name));
            Price       = price ?? throw new ArgumentNullException(nameof(price));
            Description = description;
            CategoryId  = categoryId > 0 ? categoryId : throw new ArgumentException("CategoryId must be greater than 0.");
            ImageUrl    = imageUrl;
        }

        /// <summary>
        /// Cambia el estado del producto (Activo/Inactivo).
        /// </summary>
        /// <param name="productStatusId">Nuevo ID de estado (debe ser mayor a 0).</param>
        /// <exception cref="ArgumentException">Se lanza si productStatusId no es válido.</exception>
        public void ChangeStatus(int productStatusId)
        {
            ProductStatusId = productStatusId > 0 ? productStatusId : throw new ArgumentException("ProductStatusId must be greater than 0.");
        }

        /// <summary>
        /// Asigna el embedding vectorial para búsqueda semántica.
        /// </summary>
        /// <param name="embedding">Vector de embedding generado por OpenAI.</param>
        /// <exception cref="ArgumentNullException">Se lanza si embedding es null.</exception>
        public void SetEmbedding(Pgvector.Vector embedding)
        {
            Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        }

        /// <summary>
        /// Asigna la URL de la imagen del producto.
        /// </summary>
        /// <param name="imageUrl">URL de la imagen (puede ser null).</param>
        public void SetImageUrl(string? imageUrl)
        {
            ImageUrl = imageUrl;
        }
    }
}