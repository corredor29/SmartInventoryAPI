using Api.Hubs;
using Api.Middleware;
using Infrastructure.Persistence;
using Infrastructure.Data.Seeder;
using Infrastructure;

namespace Api.Extensions
{
    /// <summary>
    /// Clase estática que contiene métodos de extensión para WebApplication.
    /// Estos métodos encapsulan la configuración del pipeline de middleware,
    /// el mapeo de hubs SignalR y la siembra de datos iniciales.
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Agrega middlewares personalizados al pipeline de la aplicación.
        /// El orden es importante: ExceptionMiddleware debe estar primero para capturar
        /// todas las excepciones, seguido de RequestLoggingMiddleware para logging.
        /// </summary>
        /// <param name="app">Instancia de WebApplication donde se agregarán los middlewares.</param>
        /// <returns>La aplicación modificada para permitir encadenamiento.</returns>
        public static WebApplication UseCustomMiddleware(this WebApplication app)
        {
            // ExceptionMiddleware: Maneja globalmente todas las excepciones no controladas
            // y las convierte en respuestas JSON con formato estándar { success, message }
            // Debe estar primero en el pipeline para capturar errores de middlewares posteriores
            app.UseMiddleware<ExceptionMiddleware>();

            // RequestLoggingMiddleware: Registra información de cada request HTTP entrante
            // (método, ruta, IP, usuario autenticado, tiempo de respuesta)
            // Útil para auditoría y monitoreo de la API
            app.UseMiddleware<RequestLoggingMiddleware>();

            return app;
        }

        /// <summary>
        /// Mapea los hubs de SignalR para comunicación en tiempo real.
        /// Los hubs permiten que el servidor envíe mensajes push a los clientes conectados.
        /// </summary>
        /// <param name="app">Instancia de WebApplication donde se mapearán los hubs.</param>
        /// <returns>La aplicación modificada para permitir encadenamiento.</returns>
        public static WebApplication MapCustomHubs(this WebApplication app)
        {
            // Mapea el hub ChatHub a la ruta /hubs/chat
            // Los clientes pueden conectarse a esta ruta para recibir notificaciones en tiempo real:
            // - Notificaciones de escalamientos de chat a asesores
            // - Actualizaciones de estado de sesiones de chat
            // - Notificaciones generales a grupos de usuarios
            app.MapHub<ChatHub>("/hubs/chat");

            return app;
        }

        /// <summary>
        /// Ejecuta el seeder de base de datos para poblar datos iniciales.
        /// Crea un scope temporal para resolver servicios y ejecuta el seeder de forma idempotente.
        /// </summary>
        /// <param name="app">Instancia de WebApplication desde donde se obtendrán los servicios.</param>
        /// <returns>La aplicación modificada para permitir encadenamiento.</returns>
        /// <remarks>
        /// Este método debe ejecutarse después de que la aplicación esté construida
        /// pero antes de que empiece a escuchar requests (antes de app.Run()).
        /// El seeder verifica si los datos ya existen antes de insertar para evitar duplicados.
        /// </remarks>
        public static async Task<WebApplication> SeedDatabaseAsync(this WebApplication app)
        {
            // Crea un scope temporal para resolver servicios scoped como AppDbContext
            // Esto es necesario porque estamos fuera del contexto de un request HTTP
            using var scope = app.Services.CreateScope();

            // Resuelve AppDbContext desde el contenedor de dependencias
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ejecuta el seeder que pobla catálogos iniciales:
            // - Roles (Administrador, Asesor, Cliente)
            // - Categorías de productos
            // - Estados de producto, venta, chat, escalamiento
            // - Usuarios de prueba (admin, asesores)
            // El seeder es idempotente: verifica existencia antes de insertar
            await DbSeeder.SeedAsync(dbContext);

            return app;
        }
    }
}