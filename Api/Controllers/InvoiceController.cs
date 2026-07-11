using System.Security.Claims;
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

        /// <summary>
        /// Facturas del usuario autenticado (solo las suyas).
        /// </summary>
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var raw =
                User.FindFirstValue("nameid") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");
            if (!int.TryParse(raw, out var userId) || userId <= 0)
                return Unauthorized(new { message = "Sesión inválida. Vuelve a iniciar sesión." });

            var invoices = await _invoiceService.GetMineAsync(userId);
            return Ok(invoices);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            return invoice is null ? NotFound() : Ok(invoice);
        }

        [HttpGet("{id:int}/pdf")]
        [Authorize(Roles = "Administrador,Asesor")]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var pdf = await _invoiceService.GeneratePdfAsync(id);
            if (pdf is null)
                return NotFound(new { message = "No se encontro la factura." });

            return File(pdf.Value.Content, "application/pdf", pdf.Value.FileName);
        }

        [HttpGet("number/{invoiceNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByInvoiceNumber(string invoiceNumber)
        {
            var invoice = await _invoiceService.GetByInvoiceNumberAsync(invoiceNumber);
            return invoice is null
                ? NotFound(new { message = "No se encontró una factura con ese número." })
                : Ok(invoice);
        }

        /// <summary>
        /// Alias para el chatbot Python: GET /api/invoices/{invoiceNumber}
        /// (ruta canónica: GET /api/invoices/number/{invoiceNumber}).
        /// </summary>
        [HttpGet("{invoiceNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByInvoiceNumberAlias(string invoiceNumber)
        {
            // Evitar colisión con {id:int}: solo números de factura tipo FAC-...
            if (int.TryParse(invoiceNumber, out _))
                return NotFound();

            return await GetByInvoiceNumber(invoiceNumber);
        }
    }
}
