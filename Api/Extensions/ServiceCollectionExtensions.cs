using System;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Infrastructure.UnitOfWork;
using Infrastructure.ExternalServices;
using Application.Contracts.Repositories;
using Application.Contracts.Services;
using Application.Contracts.Services.Users;
using Application.Contracts.Services.Customers;
using Application.Contracts.Services.Products;
using Application.Contracts.Services.Inventories;
using Application.Contracts.Services.Sales;
using Application.Contracts.Services.Invoices;
using Application.Contracts.Services.Chats;
using Application.Contracts.Services.Dashboard;
using Application.Services;
using Application.Services.Users;
using Application.Services.Customers;
using Application.Services.Products;
using Application.Services.Inventories;
using Application.Services.Sales;
using Application.Services.Invoices;
using Application.Services.Chats;
using Application.Services.Dashboard;
using Application.Validators.Products;
using Api.Filters;
using Infrastructure;

namespace Api.Extensions
{
    /// <summary>
    /// Clase estática que contiene métodos de extensión para IServiceCollection.
    /// Estos métodos encapsulan la configuración de servicios de inyección de dependencias
    /// para mantener el código de Program.cs limpio y organizado.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configura y registra el DbContext de Entity Framework Core con PostgreSQL.
        /// Habilita el soporte para la extensión pgvector necesaria para búsqueda vectorial.
        /// </summary>
        /// <param name="services">Colección de servicios donde se registrará el DbContext.</param>
        /// <param name="configuration">Configuración de la aplicación que contiene la connection string.</param>
        /// <returns>La colección de servicios modificada para permitir encadenamiento.</returns>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // Registra AppDbContext como servicio Scoped (una instancia por request HTTP)
            // Usa Npgsql como provider para PostgreSQL
            // Habilita la extensión pgvector para soportar columnas vectoriales (embeddings)
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.UseVector()
                )
            );

            return services;
        }

        /// <summary>
        /// Registra todos los servicios de aplicación, repositorios, Unit of Work,
        /// clientes HTTP externos y validadores FluentValidation.
        /// </summary>
        /// <param name="services">Colección de servicios donde se registrarán los servicios de aplicación.</param>
        /// <returns>La colección de servicios modificada para permitir encadenamiento.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // ==============================================================================
            // UNIT OF WORK Y REPOSITORIOS
            // ==============================================================================
            // Registra Unit of Work como Scoped para agrupar operaciones de repositorios
            // en una sola transacción. Scoped asegura que todos los repositorios en un
            // request compartan la misma instancia de DbContext.
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            // ==============================================================================
            // SERVICIOS DE AUTENTICACIÓN
            // ==============================================================================
            // TokenService: Genera y valida tokens JWT
            services.AddScoped<ITokenService, TokenService>();
            // AuthService: Maneja login, registro y vinculación de usuarios con clientes
            services.AddScoped<IAuthService, AuthService>();

            services.AddHttpClient<IChatbotClient, ChatbotHttpClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(90);
            });

            // OpenAiEmbeddingService: Cliente HTTP para generar embeddings vectoriales con OpenAI
            // Usado para búsqueda semántica de productos
            services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();

            // ==============================================================================
            // SERVICIOS DE USUARIOS Y CLIENTES
            // ==============================================================================
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICustomerService, CustomerService>();

            // ==============================================================================
            // SERVICIOS DE PRODUCTOS
            // ==============================================================================
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductStatusService, ProductStatusService>();
            services.AddScoped<IProductService, ProductService>();

            // ==============================================================================
            // SERVICIOS DE INVENTARIO
            // ==============================================================================
            services.AddScoped<IMovementTypeService, MovementTypeService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IInventoryMovementService, InventoryMovementService>();

            // ==============================================================================
            // SERVICIOS DE VENTAS
            // ==============================================================================
            services.AddScoped<ISaleOriginService, SaleOriginService>();
            services.AddScoped<ISaleStatusService, SaleStatusService>();
            services.AddScoped<ISaleService, SaleService>();
            services.AddScoped<ISaleDetailService, SaleDetailService>();

            // ==============================================================================
            // SERVICIOS DE FACTURACIÓN
            // ==============================================================================
            services.AddScoped<IInvoiceService, InvoiceService>();

            // ==============================================================================
            // SERVICIOS DE CHATBOT
            // ==============================================================================
            services.AddScoped<IChatSessionStatusService, ChatSessionStatusService>();
            services.AddScoped<IChatSessionService, ChatSessionService>();
            services.AddScoped<ISenderTypeService, SenderTypeService>();
            services.AddScoped<IChatMessageService, ChatMessageService>();
            services.AddScoped<IEscalationStatusService, EscalationStatusService>();
            services.AddScoped<IChatEscalationService, ChatEscalationService>();

            // ==============================================================================
            // SERVICIOS DE DASHBOARD
            // ==============================================================================
            services.AddScoped<IDashboardService, DashboardService>();

            // ==============================================================================
            // VALIDADORES FLUENTVALIDATION
            // ==============================================================================
            // Escanea el ensamblado que contiene CreateProductRequestValidator y registra
            // automáticamente todos los validadores encontrados
            services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();
            // Habilita la validación automática de FluentValidation en los controladores
            // (integra con ModelState)
            services.AddFluentValidationAutoValidation();

            return services;
        }

        /// <summary>
        /// Configura la autenticación JWT Bearer con validación de tokens,
        /// claims personalizados y soporte para SignalR vía query parameter.
        /// </summary>
        /// <param name="services">Colección de servicios donde se configurará la autenticación.</param>
        /// <param name="configuration">Configuración de la aplicación que contiene las claves JWT.</param>
        /// <returns>La colección de servicios modificada para permitir encadenamiento.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no está configurado Jwt:Secret en appsettings.
        /// </exception>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Lee la configuración JWT desde appsettings.json
            // Jwt:Secret es obligatorio y debe tener al menos 32 caracteres para seguridad
            var jwtSecret = configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Secret en appsettings.");
            // Jwt:Issuer y Audience tienen valores por defecto si no están configurados
            var jwtIssuer = configuration["Jwt:Issuer"] ?? "SmartInventoryAPI";
            var jwtAudience = configuration["Jwt:Audience"] ?? "SmartInventoryClient";

            // Configura el esquema de autenticación como JWT Bearer
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // .NET 8+: MapInboundClaims=false deja los claims con nombres cortos ("role", "nameid")
                // en lugar de los prefijos estándar de JWT ("http://schemas.microsoft.com/.../role")
                options.MapInboundClaims = false;

                // Configura los parámetros de validación del token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,          // Valida que el issuer del token coincida
                    ValidateAudience = true,        // Valida que la audiencia del token coincida
                    ValidateLifetime = true,        // Valida que el token no haya expirado
                    ValidateIssuerSigningKey = true, // Valida la firma del token con la clave secreta
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    // Convierte la clave secreta en bytes para validar la firma del token
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    NameClaimType = ClaimTypes.Name,  // Claim para el nombre del usuario
                    RoleClaimType = "role",            // Claim personalizado para el rol (usado en [Authorize(Roles="...")])
                };

                // Configura eventos personalizados para el manejo de tokens JWT
                options.Events = new JwtBearerEvents
                {
                    // Evento que se ejecuta cuando se recibe un mensaje (request)
                    OnMessageReceived = context =>
                    {
                        // SignalR no puede enviar el header Authorization estándar, así que
                        // permite enviar el token vía query parameter "access_token"
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // Si el token viene en el query parameter y la ruta es un hub de SignalR
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken; // Usa el token del query parameter
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            // Registra el servicio de autorización que usa los roles del token JWT
            services.AddAuthorization();

            return services;
        }

        /// <summary>
        /// Configura la política CORS para permitir requests desde el frontend React.
        /// Habilita el envío de credenciales (cookies, headers de autorización).
        /// </summary>
        /// <param name="services">Colección de servicios donde se configurará CORS.</param>
        /// <param name="configuration">Configuración de la aplicación que contiene la URL del frontend.</param>
        /// <returns>La colección de servicios modificada para permitir encadenamiento.</returns>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            // Lee la URL del frontend desde configuración, usa localhost:5173 por defecto
            var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";

            services.AddCors(options =>
            {
                // Define una política llamada "AllowFrontend" que se usará en el pipeline
                options.AddPolicy("AllowFrontend", policy =>
                {
                    // Permite requests solo desde el origen especificado (no usar * con credenciales)
                    policy.WithOrigins(frontendUrl)
                          .AllowAnyHeader()        // Permite cualquier header (Content-Type, Authorization, etc.)
                          .AllowAnyMethod()        // Permite cualquier método HTTP (GET, POST, PUT, DELETE, etc.)
                          .AllowCredentials();     // Permite enviar credenciales (cookies, auth headers)
                });
            });

            return services;
        }

        /// <summary>
        /// Configura el rate limiting para prevenir abuso de la API.
        /// Implementa tres niveles de limitación: global, autenticación y chatbot.
        /// </summary>
        /// <param name="services">Colección de servicios donde se configurará el rate limiting.</param>
        /// <param name="configuration">Configuración de la aplicación con los límites por IP.</param>
        /// <returns>La colección de servicios modificada para permitir encadenamiento.</returns>
        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            // Lee la configuración de rate limiting con valores por defecto
            var globalPermitLimit = configuration.GetValue("RateLimiting:Global:PermitLimit", 100);
            var globalWindowMinutes = configuration.GetValue("RateLimiting:Global:WindowMinutes", 1);
            var authPermitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 5);
            var authWindowMinutes = configuration.GetValue("RateLimiting:Auth:WindowMinutes", 1);
            var chatbotPermitLimit = configuration.GetValue("RateLimiting:Chatbot:PermitLimit", 30);
            var chatbotWindowMinutes = configuration.GetValue("RateLimiting:Chatbot:WindowMinutes", 1);

            services.AddRateLimiter(options =>
            {
                // ==============================================================================
                // LIMITADOR GLOBAL (APLICA A TODOS LOS ENDPOINTS)
                // ==============================================================================
                // Usa la dirección IP como clave de partición para limitar por cliente
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = globalPermitLimit,           // Máximo de requests permitidas
                            Window = TimeSpan.FromMinutes(globalWindowMinutes), // Ventana de tiempo (1 minuto)
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst, // Procesa requests encolados en orden
                            QueueLimit = 0,                           // No permite encolar requests (rechaza inmediatamente)
                        }));

                // ==============================================================================
                // LIMITADOR ESPECÍFICO PARA AUTENTICACIÓN
                // ==============================================================================
                // Más estricto que el global para prevenir ataques de fuerza bruta
                // Se aplica con [EnableRateLimiting("auth")] en los endpoints de login/register
                options.AddFixedWindowLimiter("auth", limiterOptions =>
                {
                    limiterOptions.PermitLimit = authPermitLimit;
                    limiterOptions.Window = TimeSpan.FromMinutes(authWindowMinutes);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                });

                // ==============================================================================
                // LIMITADOR ESPECÍFICO PARA CHATBOT
                // ==============================================================================
                // Limita el uso de endpoints del chatbot para prevenir abuso del servicio de IA
                // Se aplica con [EnableRateLimiting("chatbot")] en endpoints de chat
                options.AddFixedWindowLimiter("chatbot", limiterOptions =>
                {
                    limiterOptions.PermitLimit = chatbotPermitLimit;
                    limiterOptions.Window = TimeSpan.FromMinutes(chatbotWindowMinutes);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                });

                // ==============================================================================
                // MANEJO DE RECHAZO (CUANDO SE EXCEDE EL LÍMITE)
                // ==============================================================================
                // Personaliza la respuesta cuando se excede el límite (HTTP 429)
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"message\":\"Demasiadas solicitudes. Intenta de nuevo en unos momentos.\"}",
                        cancellationToken);
                };
            });

            return services;
        }

        /// <summary>
        /// Registra los controladores MVC con un filtro de respuesta unificada.
        /// El filtro envuelve todas las respuestas en un formato estándar { success, data }.
        /// </summary>
        /// <param name="services">Colección de servicios donde se registrarán los controladores.</param>
        /// <returns>La colección de servicios modificada para permitir encadenamiento.</returns>
        public static IServiceCollection AddControllersWithUnifiedResponse(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                // Agrega UnifiedResponseFilter globalmente a todos los controladores
                // Este filtro intercepta las respuestas y las envuelve en formato estándar
                options.Filters.Add<UnifiedResponseFilter>();
            });

            return services;
        }
    }
}