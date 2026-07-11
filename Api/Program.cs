using Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Asegura wwwroot para servir imágenes subidas (/uploads/products/...)
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);
builder.Environment.WebRootPath = webRootPath;

builder.Services.AddOpenApi();
builder.Services.AddControllersWithUnifiedResponse();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddCustomRateLimiting(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCustomMiddleware();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapCustomHubs();

await app.SeedDatabaseAsync();

app.Run();