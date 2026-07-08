using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.EscalationStatus;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/escalation-statuses")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class EscalationStatusController : ControllerBase
    {
        private readonly IEscalationStatusService _escalationStatusService;

        public EscalationStatusController(IEscalationStatusService escalationStatusService)
        {
            _escalationStatusService = escalationStatusService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var statuses = await _escalationStatusService.GetAllAsync();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var status = await _escalationStatusService.GetByIdAsync(id);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CreateEscalationStatusRequest request)
        {
            var status = await _escalationStatusService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = status.EscalationStatusId }, status);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEscalationStatusRequest request)
        {
            var status = await _escalationStatusService.UpdateAsync(id, request);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _escalationStatusService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}