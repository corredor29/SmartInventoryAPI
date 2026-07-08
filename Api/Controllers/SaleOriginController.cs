using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.SaleOrigin;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/sale-origins")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class SaleOriginController : ControllerBase
    {
        private readonly ISaleOriginService _saleOriginService;

        public SaleOriginController(ISaleOriginService saleOriginService)
        {
            _saleOriginService = saleOriginService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var origins = await _saleOriginService.GetAllAsync();
            return Ok(origins);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var origin = await _saleOriginService.GetByIdAsync(id);
            return origin is null ? NotFound() : Ok(origin);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateSaleOriginRequest request)
        {
            var origin = await _saleOriginService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = origin.SaleOriginId }, origin);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSaleOriginRequest request)
        {
            var origin = await _saleOriginService.UpdateAsync(id, request);
            return origin is null ? NotFound() : Ok(origin);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _saleOriginService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}