using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.Sale;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/sales")]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SaleController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetAll()
        {
            var sales = await _saleService.GetAllAsync();
            return Ok(sales);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetById(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            return sale is null ? NotFound() : Ok(sale);
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> Create([FromBody] CreateSaleRequest request)
        {
            var result = await _saleService.CreateAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { id = result.SaleId }, result);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeSaleStatusRequest request)
        {
            var sale = await _saleService.ChangeStatusAsync(id, request.SaleStatusId);
            return sale is null ? NotFound() : Ok(sale);
        }
    }
}