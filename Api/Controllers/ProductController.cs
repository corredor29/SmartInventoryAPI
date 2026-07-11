using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Contracts.Services.Products;
using Application.DTOs.Products.Product;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador que expone los endpoints de gestión de productos.
    /// Permite listar, buscar, crear, actualizar, eliminar productos y subir imágenes.
    /// </summary>
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        // ==============================================================================
        // CONSTANTES - EXTENSIONES DE IMAGEN PERMITIDAS
        // ==============================================================================
        // Define las extensiones de archivo permitidas para imágenes de productos.
        // Usamos StringComparer.OrdinalIgnoreCase para que la validación sea case-insensitive.
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif",
        };

        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Constructor que inyecta las dependencias necesarias mediante inyección de dependencias.
        /// </summary>
        /// <param name="productService">Servicio que contiene la lógica de negocio de productos.</param>
        /// <param name="environment">Información del entorno de hosting para rutas de archivos.</param>
        public ProductController(IProductService productService, IWebHostEnvironment environment)
        {
            _productService = productService;
            _environment = environment;
        }

        /// <summary>
        /// Endpoint para listar todos los productos del catálogo.
        /// No requiere autenticación (acceso público para clientes).
        /// </summary>
        /// <returns>HTTP 200 con la lista de todos los productos.</returns>
        [HttpGet]
        [AllowAnonymous] // Permite acceso sin autenticación (público)
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        /// <summary>
        /// Endpoint para obtener un producto específico por su ID.
        /// No requiere autenticación (acceso público para clientes).
        /// </summary>
        /// <param name="id">ID del producto a buscar.</param>
        /// <returns>
        /// HTTP 200 con los detalles del producto si existe.
        /// HTTP 404 si el producto no existe.
        /// </returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous] // Permite acceso sin autenticación (público)
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            return product is null ? NotFound() : Ok(product);
        }

        /// <summary>
        /// Endpoint de búsqueda semántica de productos usando pgvector.
        /// Si hay API key de OpenAI configurada, usa embeddings vectoriales.
        /// Si no, hace fallback a búsqueda por texto.
        /// </summary>
        /// <param name="q">Query de búsqueda (texto del producto a buscar).</param>
        /// <returns>
        /// HTTP 200 con la lista de productos encontrados y un flag "found".
        /// HTTP 400 si el parámetro 'q' está vacío.
        /// </returns>
        /// <remarks>
        /// Este endpoint tiene rate limiting aplicado (30 requests por minuto por IP)
        /// para prevenir abuso del servicio de búsqueda semántica.
        /// Es usado principalmente por el chatbot para encontrar productos relevantes.
        /// </remarks>
        [HttpGet("search")]
        [AllowAnonymous] // Permite acceso sin autenticación (usado por chatbot)
        [EnableRateLimiting("chatbot")] // Aplica el limitador "chatbot" configurado en ServiceCollectionExtensions
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            // Valida que el parámetro de búsqueda no esté vacío
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "El parametro 'q' es requerido." });

            // Delega la búsqueda al servicio (usa pgvector si hay API key, fallback a texto si no)
            var products = await _productService.SearchAsync(q);
            return Ok(new { found = products.Count > 0, products });
        }

        /// <summary>
        /// Endpoint para regenerar los embeddings vectoriales de todos los productos.
        /// Usa la API de OpenAI para generar embeddings de búsqueda semántica.
        /// </summary>
        /// <returns>
        /// HTTP 200 con mensaje de éxito y cantidad de productos actualizados.
        /// HTTP 400 si no está configurada la API key de OpenAI.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere rol Administrador
        /// - Necesita OpenAI:ApiKey o variable de entorno OPENAI_API_KEY configurada
        /// - Recorre todos los productos y genera embeddings usando el modelo text-embedding-3-small
        /// - Útil cuando se agregan nuevos productos o se actualizan descripciones
        /// </remarks>
        [HttpPost("reindex-embeddings")]
        [Authorize(Roles = "Administrador")] // Solo administradores pueden regenerar embeddings
        public async Task<IActionResult> ReindexEmbeddings()
        {
            try
            {
                // Llama al servicio para regenerar embeddings de todos los productos
                var updated = await _productService.ReindexEmbeddingsAsync();
                return Ok(new
                {
                    message = "Embeddings actualizados.",
                    updated,
                });
            }
            catch (InvalidOperationException ex)
            {
                // Si no está configurada la API key de OpenAI, retorna error 400
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para subir una imagen de producto.
        /// Guarda el archivo en wwwroot/uploads/products/ y devuelve la URL pública.
        /// </summary>
        /// <param name="file">Archivo de imagen a subir (multipart/form-data).</param>
        /// <returns>
        /// HTTP 200 con la URL pública de la imagen (/uploads/products/{filename}).
        /// HTTP 400 si el archivo es inválido, excede el tamaño o tiene formato incorrecto.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere rol Administrador
        /// - Acepta solo imágenes JPG, PNG, WEBP o GIF
        /// - Límite de tamaño: 5 MB
        /// - Genera un nombre de archivo único con GUID para evitar colisiones
        /// - La imagen queda accesible públicamente en /uploads/products/
        /// </remarks>
        [HttpPost("upload-image")]
        [Authorize(Roles = "Administrador")] // Solo administradores pueden subir imágenes
        [Consumes("multipart/form-data")] // Indica que consume datos multipart/form-data
        [RequestSizeLimit(5 * 1024 * 1024)] // Límite de tamaño de request: 5 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)] // Límite de cuerpo multipart: 5 MB
        public async Task<IActionResult> UploadImage([FromForm] IFormFile? file)
        {
            // Si el parámetro file es null, intenta obtener el primer archivo del form
            file ??= Request.Form.Files.FirstOrDefault();

            // Valida que se haya proporcionado un archivo
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Selecciona una imagen." });

            // Valida que el archivo no supere los 5 MB
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "La imagen no puede superar 5 MB." });

            // Valida que la extensión del archivo sea permitida
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
                return BadRequest(new { message = "Formato no permitido. Usa JPG, PNG, WEBP o GIF." });

            // Valida el Content-Type si viene (algunos navegadores lo mandan vacío)
            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "El archivo debe ser una imagen." });
            }

            // ==============================================================================
            // DETERMINACIÓN DE RUTA DE ALMACENAMIENTO
            // ==============================================================================
            // Usa WebRootPath si está configurado, sino usa ContentRootPath + wwwroot
            // Esto asegura que la ruta sea estable independientemente del entorno
            var webRoot = !string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? _environment.WebRootPath
                : Path.Combine(_environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot); // Crea el directorio si no existe

            // Crea el directorio uploads/products si no existe
            var uploadsDir = Path.Combine(webRoot, "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            // ==============================================================================
            // GUARDADO DEL ARCHIVO
            // ==============================================================================
            // Genera un nombre de archivo único usando GUID + extensión en minúsculas
            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            // Copia el contenido del archivo al disco
            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            // Retorna la URL pública relativa del archivo
            var relativeUrl = $"/uploads/products/{fileName}";
            return Ok(new UploadImageResponse { ImageUrl = relativeUrl });
        }

        /// <summary>
        /// Endpoint para crear un nuevo producto.
        /// Crea automáticamente el inventario asociado con stock inicial.
        /// </summary>
        /// <param name="request">DTO con los datos del producto a crear.</param>
        /// <returns>
        /// HTTP 201 con el producto creado y header Location apuntando a GET /api/products/{id}.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere rol Administrador
        /// - Crea el producto con los datos proporcionados
        /// - Genera automáticamente el Inventory asociado
        /// - Si hay stock inicial > 0, registra un movimiento de entrada
        /// - Si hay API key de OpenAI, genera el embedding automáticamente
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Administrador")] // Solo administradores pueden crear productos
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            var product = await _productService.CreateAsync(request);
            // Retorna 201 Created con header Location apuntando al endpoint GET por ID
            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
        }

        /// <summary>
        /// Endpoint para actualizar un producto existente.
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="request">DTO con los datos actualizados del producto.</param>
        /// <returns>
        /// HTTP 200 con el producto actualizado.
        /// HTTP 404 si el producto no existe.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere rol Administrador
        /// - Si se cambió nombre/descripción/categoría, regenera el embedding automáticamente
        /// </remarks>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")] // Solo administradores pueden actualizar productos
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            var product = await _productService.UpdateAsync(id, request);
            return product is null ? NotFound() : Ok(product);
        }

        /// <summary>
        /// Endpoint para cambiar el estado de un producto (Activo/Inactivo).
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="productStatusId">ID del nuevo estado de producto.</param>
        /// <returns>
        /// HTTP 200 con el producto actualizado.
        /// HTTP 404 si el producto no existe.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere rol Administrador
        /// - Permite activar/desactivar productos sin modificar otros datos
        /// - Útil para gestión de catálogo (productos fuera de stock, discontinuados, etc.)
        /// </remarks>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Administrador")] // Solo administradores pueden cambiar estado
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] int productStatusId)
        {
            var product = await _productService.ChangeStatusAsync(id, productStatusId);
            return product is null ? NotFound() : Ok(product);
        }

        /// <summary>
        /// Endpoint para eliminar un producto.
        /// </summary>
        /// <param name="id">ID del producto a eliminar.</param>
        /// <returns>
        /// HTTP 204 No Content si el producto fue eliminado.
        /// HTTP 404 si el producto no existe.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere rol Administrador
        /// - Elimina el producto y su inventario asociado
        /// - Cuidado: esta operación no puede deshacerse
        /// </remarks>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")] // Solo administradores pueden eliminar productos
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        /// <summary>
        /// DTO interno para la respuesta de subida de imagen.
        /// Contiene solo la URL pública de la imagen subida.
        /// </summary>
        private sealed class UploadImageResponse
        {
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
