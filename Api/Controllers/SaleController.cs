using System.Security.Claims;
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

        /// <summary>
        /// Pedidos del usuario autenticado (solo los suyos, filtrados por customer vinculado).
        /// </summary>
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = TryGetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Sesión inválida. Vuelve a iniciar sesión." });

            var sales = await _saleService.GetMineAsync(userId.Value);
            return Ok(sales);
        }

        [HttpGet("{id:int}")]
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
            var authenticatedUserId = TryGetAuthenticatedUserId();
            var result = await _saleService.CreateAsync(request, authenticatedUserId);

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

        private int? TryGetAuthenticatedUserId()
        {
            // Compatibilidad: JWT corto ("nameid") y ClaimTypes largo.
            var raw =
                User.FindFirstValue("nameid") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");
            return int.TryParse(raw, out var userId) && userId > 0 ? userId : null;
        }
    }
}
