using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Contracts.Services.Sales;
using Application.DTOs.Sales.Sale;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador que expone los endpoints de gestión de ventas.
    /// Permite listar ventas, crear pedidos (manual o vía chatbot), cambiar estados y consultar pedidos del usuario.
    /// </summary>
    [ApiController]
    [Route("api/sales")]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _saleService;

        /// <summary>
        /// Constructor que inyecta el servicio de ventas mediante inyección de dependencias.
        /// </summary>
        /// <param name="saleService">Servicio que contiene la lógica de negocio de ventas.</param>
        public SaleController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        /// <summary>
        /// Endpoint para listar todas las ventas del sistema.
        /// Solo accesible por administradores y asesores.
        /// </summary>
        /// <returns>HTTP 200 con la lista de todas las ventas.</returns>
        [HttpGet]
        [Authorize(Roles = "Administrador,Asesor")] // Solo administradores y asesores pueden ver todas las ventas
        public async Task<IActionResult> GetAll()
        {
            var sales = await _saleService.GetAllAsync();
            return Ok(sales);
        }

        /// <summary>
        /// Endpoint para obtener las ventas del usuario autenticado.
        /// Filtra las ventas por el Customer vinculado al usuario.
        /// </summary>
        /// <returns>
        /// HTTP 200 con la lista de ventas del usuario.
        /// HTTP 401 si no hay un usuario autenticado válido.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere autenticación (cualquier rol)
        /// - Filtra las ventas por el Customer vinculado al usuario autenticado
        /// - Útil para que los clientes vean su historial de pedidos
        /// </remarks>
        [HttpGet("mine")]
        [Authorize] // Requiere autenticación (cualquier rol)
        public async Task<IActionResult> GetMine()
        {
            // Obtiene el ID del usuario autenticado desde los claims del JWT
            var userId = TryGetAuthenticatedUserId();
            if (userId is null)
                return Unauthorized(new { message = "Sesión inválida. Vuelve a iniciar sesión." });

            // Obtiene las ventas filtradas por el Customer vinculado al usuario
            var sales = await _saleService.GetMineAsync(userId.Value);
            return Ok(sales);
        }

        /// <summary>
        /// Endpoint para obtener una venta específica por su ID.
        /// Solo accesible por administradores y asesores.
        /// </summary>
        /// <param name="id">ID de la venta a buscar.</param>
        /// <returns>
        /// HTTP 200 con los detalles de la venta si existe.
        /// HTTP 404 si la venta no existe.
        /// </returns>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Administrador,Asesor")] // Solo administradores y asesores pueden ver cualquier venta
        public async Task<IActionResult> GetById(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            return sale is null ? NotFound() : Ok(sale);
        }

        /// <summary>
        /// Endpoint para crear una nueva venta.
        /// Puede ser llamado por usuarios autenticados o anónimos (chatbot).
        /// Valida el stock disponible antes de crear la venta.
        /// </summary>
        /// <param name="request">DTO con los datos de la venta (productos, cantidades, customer).</param>
        /// <returns>
        /// HTTP 201 con la venta creada y header Location apuntando a GET /api/sales/{id}.
        /// HTTP 400 si hay error de validación (stock insuficiente, producto no existe, etc.).
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - No requiere autenticación (permite ventas vía chatbot)
        /// - Tiene rate limiting (30 requests por minuto por IP)
        /// - Valida el stock disponible de cada producto antes de crear
        /// - Si el usuario está autenticado, vincula la venta a su Customer
        /// - Si no hay usuario autenticado, usa el CustomerId del request
        /// - Genera automáticamente la factura asociada
        /// - Usa transacción para rollback si falla alguna validación
        /// </remarks>
        [HttpPost]
        [AllowAnonymous] // Permite ventas anónimas (usadas por chatbot)
        [EnableRateLimiting("chatbot")] // Aplica el limitador "chatbot" configurado en ServiceCollectionExtensions
        public async Task<IActionResult> Create([FromBody] CreateSaleRequest request)
        {
            // Obtiene el ID del usuario autenticado si existe (puede ser null para ventas anónimas)
            var authenticatedUserId = TryGetAuthenticatedUserId();
            // Crea la venta con validación de stock y generación de factura
            var result = await _saleService.CreateAsync(request, authenticatedUserId);

            // Si la creación falló (stock insuficiente, producto no existe, etc.), retorna error 400
            if (!result.Success)
                return BadRequest(result);

            // Retorna 201 Created con header Location apuntando al endpoint GET por ID
            return CreatedAtAction(nameof(GetById), new { id = result.SaleId }, result);
        }

        /// <summary>
        /// Endpoint para cambiar el estado de una venta.
        /// Solo accesible por administradores y asesores.
        /// </summary>
        /// <param name="id">ID de la venta a actualizar.</param>
        /// <param name="request">DTO con el ID del nuevo estado de venta.</param>
        /// <returns>
        /// HTTP 200 con la venta actualizada.
        /// HTTP 404 si la venta no existe.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Requiere rol Administrador o Asesor
        /// - Permite cambiar el estado de una venta (Pendiente -> Completada -> Cancelada)
        /// - Útil para gestión del ciclo de vida de las ventas
        /// </remarks>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrador,Asesor")] // Solo administradores y asesores pueden cambiar estados
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeSaleStatusRequest request)
        {
            var sale = await _saleService.ChangeStatusAsync(id, request.SaleStatusId);
            return sale is null ? NotFound() : Ok(sale);
        }

        /// <summary>
        /// Método privado que intenta obtener el ID del usuario autenticado desde los claims del JWT.
        /// Soporta múltiples formatos de claim para compatibilidad con diferentes configuraciones de JWT.
        /// </summary>
        /// <returns>
        /// ID del usuario autenticado si existe y es válido.
        /// Null si no hay usuario autenticado o el claim es inválido.
        /// </returns>
        /// <remarks>
        /// Este método busca el claim de ID del usuario en múltiples formatos:
        /// - "nameid": claim corto estándar de JWT (usado cuando MapInboundClaims=false)
        /// - ClaimTypes.NameIdentifier: claim largo estándar de .NET
        /// - "sub": claim estándar de OAuth 2.0
        /// Esto asegura compatibilidad con diferentes configuraciones de autenticación JWT.
        /// </remarks>
        private int? TryGetAuthenticatedUserId()
        {
            // Intenta obtener el claim en diferentes formatos para compatibilidad
            var raw =
                User.FindFirstValue("nameid") ??                      // Claim corto (JWT estándar)
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??     // Claim largo (.NET estándar)
                User.FindFirstValue("sub");                           // Claim OAuth 2.0 estándar

            // Intenta parsear el valor a entero y valida que sea positivo
            return int.TryParse(raw, out var userId) && userId > 0 ? userId : null;
        }
    }
}
