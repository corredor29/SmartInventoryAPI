using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Contracts.Services.Inventories;
using Application.DTOs.Inventories.Inventory;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var inventories = await _inventoryService.GetAllAsync();
            return Ok(inventories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var inventory = await _inventoryService.GetByIdAsync(id);
            return inventory is null ? NotFound() : Ok(inventory);
        }

        [HttpGet("{productId}/stock")]
        [AllowAnonymous]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var inventory = await _inventoryService.GetByProductIdAsync(productId);

            if (inventory is null)
                return NotFound(new { message = "No se encontró inventario para este producto." });

            return Ok(new { current_stock = inventory.CurrentStock });
        }

        [HttpPatch("{productId}/adjust")]
        public async Task<IActionResult> AdjustStock(int productId, [FromBody] UpdateInventoryRequest request)
        {
            var inventory = await _inventoryService.AdjustStockAsync(productId, request);
            return inventory is null ? NotFound() : Ok(inventory);
        }
    }
}