using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.SaleStatus;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/sale-statuses")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class SaleStatusController : ControllerBase
    {
        private readonly ISaleStatusService _saleStatusService;

        public SaleStatusController(ISaleStatusService saleStatusService)
        {
            _saleStatusService = saleStatusService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var statuses = await _saleStatusService.GetAllAsync();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var status = await _saleStatusService.GetByIdAsync(id);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateSaleStatusRequest request)
        {
            var status = await _saleStatusService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = status.SaleStatusId }, status);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSaleStatusRequest request)
        {
            var status = await _saleStatusService.UpdateAsync(id, request);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _saleStatusService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}