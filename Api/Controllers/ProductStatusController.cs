using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Products;
using Application.DTOs.Products.ProductStatus;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/product-statuses")]
    public class ProductStatusController : ControllerBase
    {
        private readonly IProductStatusService _productStatusService;

        public ProductStatusController(IProductStatusService productStatusService)
        {
            _productStatusService = productStatusService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var statuses = await _productStatusService.GetAllAsync();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var status = await _productStatusService.GetByIdAsync(id);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateProductStatusRequest request)
        {
            var status = await _productStatusService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = status.ProductStatusId }, status);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductStatusRequest request)
        {
            var status = await _productStatusService.UpdateAsync(id, request);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productStatusService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}