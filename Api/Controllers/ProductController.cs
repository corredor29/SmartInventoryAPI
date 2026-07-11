using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Contracts.Services.Products;
using Application.DTOs.Products.Product;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif",
        };

        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _environment;

        public ProductController(IProductService productService, IWebHostEnvironment environment)
        {
            _productService = productService;
            _environment = environment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            return product is null ? NotFound() : Ok(product);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "El parametro 'q' es requerido." });

            var products = await _productService.SearchAsync(q);
            return Ok(new { found = products.Count > 0, products });
        }

        /// <summary>
        /// Regenera embeddings pgvector de todos los productos (busqueda semantica).
        /// Requiere OpenAI:ApiKey o OPENAI_API_KEY.
        /// </summary>
        [HttpPost("reindex-embeddings")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ReindexEmbeddings()
        {
            try
            {
                var updated = await _productService.ReindexEmbeddingsAsync();
                return Ok(new
                {
                    message = "Embeddings actualizados.",
                    updated,
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Sube una imagen de producto y devuelve la URL publica (/uploads/products/...).
        /// </summary>
        [HttpPost("upload-image")]
        [Authorize(Roles = "Administrador")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile? file)
        {
            file ??= Request.Form.Files.FirstOrDefault();

            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Selecciona una imagen." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "La imagen no puede superar 5 MB." });

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
                return BadRequest(new { message = "Formato no permitido. Usa JPG, PNG, WEBP o GIF." });

            // Algunos navegadores mandan content-type vacío; solo validamos si viene.
            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "El archivo debe ser una imagen." });
            }

            // Ruta estable: Api/wwwroot (no depender de webroot vacío en runtime)
            var webRoot = !string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? _environment.WebRootPath
                : Path.Combine(_environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);

            var uploadsDir = Path.Combine(webRoot, "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/products/{fileName}";
            return Ok(new UploadImageResponse { ImageUrl = relativeUrl });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            var product = await _productService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            var product = await _productService.UpdateAsync(id, request);
            return product is null ? NotFound() : Ok(product);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] int productStatusId)
        {
            var product = await _productService.ChangeStatusAsync(id, productStatusId);
            return product is null ? NotFound() : Ok(product);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        private sealed class UploadImageResponse
        {
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
