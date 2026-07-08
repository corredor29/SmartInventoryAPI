using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Invoices;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _invoiceService.GetAllAsync();
            return Ok(invoices);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            return invoice is null ? NotFound() : Ok(invoice);
        }
        [HttpGet("number/{invoiceNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByInvoiceNumber(string invoiceNumber)
        {
            var invoice = await _invoiceService.GetByInvoiceNumberAsync(invoiceNumber);
            return invoice is null ? NotFound(new { message = "No se encontró una factura con ese número." }) : Ok(invoice);
        }
    }
}