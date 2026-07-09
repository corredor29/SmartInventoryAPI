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
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.UseVector()
                )
            );

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddHttpClient<IChatbotClient, FastApiChatbotClient>();

            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<ICustomerService, CustomerService>();

            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductStatusService, ProductStatusService>();
            services.AddScoped<IProductService, ProductService>();

            services.AddScoped<IMovementTypeService, MovementTypeService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IInventoryMovementService, InventoryMovementService>();

            services.AddScoped<ISaleOriginService, SaleOriginService>();
            services.AddScoped<ISaleStatusService, SaleStatusService>();
            services.AddScoped<ISaleService, SaleService>();
            services.AddScoped<ISaleDetailService, SaleDetailService>();

            services.AddScoped<IInvoiceService, InvoiceService>();

            services.AddScoped<IChatSessionStatusService, ChatSessionStatusService>();
            services.AddScoped<IChatSessionService, ChatSessionService>();
            services.AddScoped<ISenderTypeService, SenderTypeService>();
            services.AddScoped<IChatMessageService, ChatMessageService>();
            services.AddScoped<IEscalationStatusService, EscalationStatusService>();
            services.AddScoped<IChatEscalationService, ChatEscalationService>();

            services.AddScoped<IDashboardService, DashboardService>();

            services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();
            services.AddFluentValidationAutoValidation();

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSecret = configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Secret en appsettings.");
            var jwtIssuer = configuration["Jwt:Issuer"] ?? "SmartInventoryAPI";
            var jwtAudience = configuration["Jwt:Audience"] ?? "SmartInventoryClient";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";

            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(frontendUrl)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }

        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var globalPermitLimit = configuration.GetValue("RateLimiting:Global:PermitLimit", 100);
            var globalWindowMinutes = configuration.GetValue("RateLimiting:Global:WindowMinutes", 1);
            var authPermitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 5);
            var authWindowMinutes = configuration.GetValue("RateLimiting:Auth:WindowMinutes", 1);
            var chatbotPermitLimit = configuration.GetValue("RateLimiting:Chatbot:PermitLimit", 30);
            var chatbotWindowMinutes = configuration.GetValue("RateLimiting:Chatbot:WindowMinutes", 1);

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = globalPermitLimit,
                            Window = TimeSpan.FromMinutes(globalWindowMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        }));

                options.AddFixedWindowLimiter("auth", limiterOptions =>
                {
                    limiterOptions.PermitLimit = authPermitLimit;
                    limiterOptions.Window = TimeSpan.FromMinutes(authWindowMinutes);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("chatbot", limiterOptions =>
                {
                    limiterOptions.PermitLimit = chatbotPermitLimit;
                    limiterOptions.Window = TimeSpan.FromMinutes(chatbotWindowMinutes);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                });

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

        public static IServiceCollection AddControllersWithUnifiedResponse(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<UnifiedResponseFilter>();
            });

            return services;
        }
    }
}