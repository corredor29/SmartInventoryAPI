using Api.Extensions;

/// <summary>
/// Punto de entrada principal de la aplicación SmartInventoryAPI.
/// Configura el servidor web, los servicios de inyección de dependencias,
/// el pipeline de middleware y los endpoints HTTP/SignalR.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// CONFIGURACIÓN DEL DIRECTORIO WWWROOT PARA ARCHIVOS ESTÁTICOS
// ==============================================================================
// Se configura el directorio wwwroot para servir imágenes de productos
// subidas por los usuarios. Esto es necesario porque en algunos entornos
// el WebRootPath puede no estar configurado correctamente por defecto.
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath); // Crea el directorio si no existe
builder.Environment.WebRootPath = webRootPath; // Establece la ruta raíz para archivos estáticos

// ==============================================================================
// REGISTRO DE SERVICIOS EN EL CONTENEDOR DE DEPENDENCIAS (DI)
// ==============================================================================

// Registra el servicio de documentación OpenAPI para generar especificaciones
// de la API automáticamente. Solo se expone en entorno de desarrollo.
builder.Services.AddOpenApi();

// Registra los controladores con un filtro de respuesta unificada que envuelve
// todas las respuestas en un formato estándar { success, data } o { success, message }
builder.Services.AddControllersWithUnifiedResponse();

// Configura y registra el DbContext de Entity Framework Core con PostgreSQL
// y el soporte para la extensión pgvector (búsqueda vectorial)
builder.Services.AddDatabase(builder.Configuration);

// Registra todos los servicios de aplicación (lógica de negocio), repositorios,
// Unit of Work, clientes HTTP externos y validadores FluentValidation
builder.Services.AddApplicationServices();

// Configura la política CORS para permitir requests desde el frontend React
// especificado en la configuración (por defecto http://localhost:5173)
builder.Services.AddCorsPolicy(builder.Configuration);

// Configura el rate limiting para prevenir abuso de la API:
// - Límite global por IP
// - Límite específico para endpoints de autenticación
// - Límite específico para endpoints usados por el chatbot
builder.Services.AddCustomRateLimiting(builder.Configuration);

// Registra el servicio SignalR para comunicación en tiempo real
// (usado para notificaciones de escalamientos a asesores)
builder.Services.AddSignalR();

// Configura la autenticación JWT Bearer con validación de tokens,
// claims personalizados y soporte para SignalR vía query parameter
builder.Services.AddJwtAuthentication(builder.Configuration);

// ==============================================================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ==============================================================================
var app = builder.Build();

// ==============================================================================
// CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARE
// ==============================================================================

// En entorno de desarrollo, expone el documento OpenAPI en JSON
// para que herramientas como Postman puedan importar la especificación
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Agrega middlewares personalizados:
// - ExceptionMiddleware: Manejo global de excepciones
// - RequestLoggingMiddleware: Logging de todas las requests HTTP
app.UseCustomMiddleware();

// Redirige automáticamente todas las solicitudes HTTP a HTTPS
// (importante para seguridad en producción)
app.UseHttpsRedirection();

// Habilita la política CORS configurada anteriormente
// para permitir requests desde el frontend
app.UseCors("AllowFrontend");

// Habilita el servicio de archivos estáticos para servir
// imágenes de productos desde wwwroot/uploads/products/
app.UseStaticFiles();

// Habilita el middleware de rate limiting configurado anteriormente
app.UseRateLimiter();

// Habilita la autenticación (debe estar antes de autorización)
// Valida tokens JWT en el header Authorization: Bearer {token}
app.UseAuthentication();

// Habilita la autorización
// Verifica que el usuario autenticado tenga los roles necesarios
// según los atributos [Authorize] en los controladores
app.UseAuthorization();

// ==============================================================================
// MAPEO DE ENDPOINTS
// ==============================================================================

// Mapea todos los controladores REST a sus rutas correspondientes
// (ej: /api/auth/login, /api/products, etc.)
app.MapControllers();

// Mapea los hubs de SignalR para comunicación en tiempo real
// (ej: /hubs/chat para el hub ChatHub)
app.MapCustomHubs();

// ==============================================================================
// SIEMBRA DE DATOS INICIALES
// ==============================================================================
// Ejecuta el seeder de base de datos para poblar catálogos iniciales
// (roles, categorías, estados, usuarios de prueba) de forma idempotente.
// Esto se ejecuta en cada arranque pero verifica si los datos ya existen.
await app.SeedDatabaseAsync();

// ==============================================================================
// INICIO DE LA APLICACIÓN
// ==============================================================================
// Inicia el servidor web y comienza a escuchar solicitudes HTTP
app.Run();