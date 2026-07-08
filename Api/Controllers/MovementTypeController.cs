using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Inventories;
using Application.DTOs.Inventories.MovementType;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/movement-types")]
    public class MovementTypeController : ControllerBase
    {
        private readonly IMovementTypeService _movementTypeService;

        public MovementTypeController(IMovementTypeService movementTypeService)
        {
            _movementTypeService = movementTypeService;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetAll()
        {
            var types = await _movementTypeService.GetAllAsync();
            return Ok(types);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetById(int id)
        {
            var type = await _movementTypeService.GetByIdAsync(id);
            return type is null ? NotFound() : Ok(type);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateMovementTypeRequest request)
        {
            var type = await _movementTypeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = type.MovementTypeId }, type);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMovementTypeRequest request)
        {
            var type = await _movementTypeService.UpdateAsync(id, request);
            return type is null ? NotFound() : Ok(type);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _movementTypeService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}