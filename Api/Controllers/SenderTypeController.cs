using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.SenderType;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/sender-types")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class SenderTypeController : ControllerBase
    {
        private readonly ISenderTypeService _senderTypeService;

        public SenderTypeController(ISenderTypeService senderTypeService)
        {
            _senderTypeService = senderTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var types = await _senderTypeService.GetAllAsync();
            return Ok(types);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var type = await _senderTypeService.GetByIdAsync(id);
            return type is null ? NotFound() : Ok(type);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateSenderTypeRequest request)
        {
            var type = await _senderTypeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = type.SenderTypeId }, type);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSenderTypeRequest request)
        {
            var type = await _senderTypeService.UpdateAsync(id, request);
            return type is null ? NotFound() : Ok(type);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _senderTypeService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}