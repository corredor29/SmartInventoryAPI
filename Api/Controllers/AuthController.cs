using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Contracts.Services;
using Application.DTOs.Auth;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador que expone los endpoints de autenticación de la API.
    /// Maneja el login de usuarios existentes y el registro de nuevos usuarios.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Constructor que inyecta el servicio de autenticación mediante inyección de dependencias.
        /// </summary>
        /// <param name="authService">Servicio que contiene la lógica de negocio para autenticación.</param>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Endpoint de login para autenticar usuarios existentes.
        /// Valida las credenciales y genera un token JWT si son correctas.
        /// </summary>
        /// <param name="request">DTO con email y contraseña del usuario.</param>
        /// <returns>
        /// HTTP 200 con el token JWT y datos del usuario si las credenciales son válidas.
        /// HTTP 401 si el email o contraseña son incorrectos.
        /// </returns>
        /// <remarks>
        /// Este endpoint tiene rate limiting aplicado para prevenir ataques de fuerza bruta.
        /// El límite es de 5 requests por minuto por dirección IP.
        /// </remarks>
        [HttpPost("login")]
        [EnableRateLimiting("auth")] // Aplica el limitador "auth" configurado en ServiceCollectionExtensions
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // Delega la validación de credenciales y generación de token al servicio de autenticación
            var result = await _authService.LoginAsync(request);

            // Si el servicio retorna null, significa que las credenciales son inválidas
            if (result is null)
                return Unauthorized(new { message = "Email o contraseña incorrectos." });

            // Credenciales válidas: retorna el token JWT y datos del usuario
            return Ok(result);
        }

        /// <summary>
        /// Endpoint de registro para crear nuevos usuarios.
        /// Crea un usuario con rol Cliente por defecto y genera un token JWT.
        /// </summary>
        /// <param name="request">DTO con nombre, email y contraseña del nuevo usuario.</param>
        /// <returns>
        /// HTTP 200 con el token JWT y datos del usuario si el registro es exitoso.
        /// HTTP 400 si ya existe una cuenta con el mismo email.
        /// </returns>
        /// <remarks>
        /// Este endpoint:
        /// - Crea automáticamente una entidad Customer vinculada al usuario
        /// - Asigna el rol "Cliente" por defecto
        - Hashea la contraseña con BCrypt antes de almacenarla
        /// - Tiene rate limiting para prevenir abuso del registro
        /// </remarks>
        [HttpPost("register")]
        [EnableRateLimiting("auth")] // Aplica el limitador "auth" configurado en ServiceCollectionExtensions
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // Delega la creación del usuario y generación de token al servicio de autenticación
            var result = await _authService.RegisterAsync(request);

            // Si el servicio retorna null, significa que ya existe un usuario con ese email
            if (result is null)
                return BadRequest(new { message = "Ya existe una cuenta registrada con ese email." });

            // Registro exitoso: retorna el token JWT y datos del usuario creado
            return Ok(result);
        }
    }
}