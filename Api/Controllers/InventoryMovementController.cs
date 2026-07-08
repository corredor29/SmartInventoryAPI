using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Inventories;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/inventory-movements")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class InventoryMovementController : ControllerBase
    {
        private readonly IInventoryMovementService _inventoryMovementService;

        public InventoryMovementController(IInventoryMovementService inventoryMovementService)
        {
            _inventoryMovementService = inventoryMovementService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var movements = await _inventoryMovementService.GetAllAsync();
            return Ok(movements);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var movement = await _inventoryMovementService.GetByIdAsync(id);
            return movement is null ? NotFound() : Ok(movement);
        }

        [HttpGet("by-inventory/{inventoryId}")]
        public async Task<IActionResult> GetByInventoryId(int inventoryId)
        {
            var movements = await _inventoryMovementService.GetByInventoryIdAsync(inventoryId);
            return Ok(movements);
        }
    }
}