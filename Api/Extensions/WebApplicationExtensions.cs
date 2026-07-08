using Api.Hubs;
using Api.Middleware;
using Infrastructure.Persistence;
using Infrastructure.Data.Seeder;
using Infrastructure;

namespace Api.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseCustomMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();

            return app;
        }

        public static WebApplication MapCustomHubs(this WebApplication app)
        {
            app.MapHub<ChatHub>("/hubs/chat");

            return app;
        }

        public static async Task<WebApplication> SeedDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbSeeder.SeedAsync(dbContext);

            return app;
        }
    }
}