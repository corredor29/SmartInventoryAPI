using System.Text;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Infrastructure.UnitOfWork;
using Application.Contracts.Repositories;
using Application.Contracts.Services;
using Application.Contracts.Services.Users;
using Application.Contracts.Services.Customers;
using Application.Contracts.Services.Products;
using Application.Contracts.Services.Inventories;
using Application.Contracts.Services.Sales;
using Application.Contracts.Services.Invoices;
using Application.Contracts.Services.Chats;
using Application.Services;
using Application.Services.Users;
using Application.Services.Customers;
using Application.Services.Products;
using Application.Services.Inventories;
using Application.Services.Sales;
using Application.Services.Invoices;
using Application.Services.Chats;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.UseVector()
    )
);


builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// ==========================================
// Autenticación / Autorización
// ==========================================
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ==========================================
// Servicios - Users
// ==========================================
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();

// ==========================================
// Servicios - Customers
// ==========================================
builder.Services.AddScoped<ICustomerService, CustomerService>();


builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductStatusService, ProductStatusService>();
builder.Services.AddScoped<IProductService, ProductService>();


builder.Services.AddScoped<IMovementTypeService, MovementTypeService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryMovementService, InventoryMovementService>();


builder.Services.AddScoped<ISaleOriginService, SaleOriginService>();
builder.Services.AddScoped<ISaleStatusService, SaleStatusService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<ISaleDetailService, SaleDetailService>();


builder.Services.AddScoped<IInvoiceService, InvoiceService>();


builder.Services.AddScoped<IChatSessionStatusService, ChatSessionStatusService>();
builder.Services.AddScoped<IChatSessionService, ChatSessionService>();
builder.Services.AddScoped<ISenderTypeService, SenderTypeService>();
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();
builder.Services.AddScoped<IEscalationStatusService, EscalationStatusService>();
builder.Services.AddScoped<IChatEscalationService, ChatEscalationService>();

// ==========================================
// Autenticación JWT
// ==========================================
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Secret en appsettings.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SmartInventoryAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SmartInventoryClient";

builder.Services.AddAuthentication(options =>
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
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();