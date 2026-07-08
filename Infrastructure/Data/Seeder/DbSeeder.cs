using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities.Users;
using Domain.Entities.Products;
using Domain.Entities.Inventories;
using Domain.Entities.Sales;
using Domain.Entities.Chats;
using Domain.ValueObject.Users.Role;
using Domain.ValueObject.Users.User;
using Domain.ValueObject.Products.Category;
using Domain.ValueObject.Products.ProductStatus;
using Domain.ValueObject.Products.MovementType;
using Domain.ValueObject.Sales.SaleOrigin;
using Domain.ValueObject.Sales.SaleStatus;
using Domain.ValueObject.Chats.ChatSessionStatus;
using Domain.ValueObject.Chats.SenderType;
using Domain.ValueObject.Chats.EscalationStatus;

namespace Infrastructure.Data.Seeder
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await SeedRolesAsync(context);
            await SeedProductStatusesAsync(context);
            await SeedCategoriesAsync(context);
            await SeedMovementTypesAsync(context);
            await SeedSaleOriginsAsync(context);
            await SeedSaleStatusesAsync(context);
            await SeedChatSessionStatusesAsync(context);
            await SeedSenderTypesAsync(context);
            await SeedEscalationStatusesAsync(context);
            await SeedAdminUserAsync(context);
        }

        private static async Task SeedRolesAsync(AppDbContext context)
        {
            if (await context.Roles.AnyAsync()) return;

            context.Roles.AddRange(
                new Role(new RoleName("Administrador")),
                new Role(new RoleName("Asesor")),
                new Role(new RoleName("Cliente"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedProductStatusesAsync(AppDbContext context)
        {
            if (await context.ProductStatuses.AnyAsync()) return;

            context.ProductStatuses.AddRange(
                new ProductStatus(ProductStatusName.Create("Activo")),
                new ProductStatus(ProductStatusName.Create("Inactivo"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedCategoriesAsync(AppDbContext context)
        {
            if (await context.Categories.AnyAsync()) return;

            context.Categories.AddRange(
                new Category(CategoryName.Create("Laptops")),
                new Category(CategoryName.Create("Periféricos")),
                new Category(CategoryName.Create("Componentes")),
                new Category(CategoryName.Create("Accesorios"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedMovementTypesAsync(AppDbContext context)
        {
            if (await context.MovementTypes.AnyAsync()) return;

            context.MovementTypes.AddRange(
                new MovementType(MovementTypeName.Create("Entrada")),
                new MovementType(MovementTypeName.Create("Salida")),
                new MovementType(MovementTypeName.Create("Ajuste"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedSaleOriginsAsync(AppDbContext context)
        {
            if (await context.SaleOrigins.AnyAsync()) return;

            context.SaleOrigins.AddRange(
                new SaleOrigin(SaleOriginName.Create("Manual")),
                new SaleOrigin(SaleOriginName.Create("Chatbot"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedSaleStatusesAsync(AppDbContext context)
        {
            if (await context.SaleStatuses.AnyAsync()) return;

            context.SaleStatuses.AddRange(
                new SaleStatus(SaleStatusName.Create("Pendiente")),
                new SaleStatus(SaleStatusName.Create("Completada")),
                new SaleStatus(SaleStatusName.Create("Cancelada"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedChatSessionStatusesAsync(AppDbContext context)
        {
            if (await context.ChatSessionStatuses.AnyAsync()) return;

            context.ChatSessionStatuses.AddRange(
                new ChatSessionStatus(ChatSessionStatusName.Create("Activa")),
                new ChatSessionStatus(ChatSessionStatusName.Create("Escalada")),
                new ChatSessionStatus(ChatSessionStatusName.Create("Cerrada"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedSenderTypesAsync(AppDbContext context)
        {
            if (await context.SenderTypes.AnyAsync()) return;

            context.SenderTypes.AddRange(
                new SenderType(new SenderTypeName("Bot")),
                new SenderType(new SenderTypeName("Cliente")),
                new SenderType(new SenderTypeName("Asesor"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedEscalationStatusesAsync(AppDbContext context)
        {
            if (await context.EscalationStatuses.AnyAsync()) return;

            context.EscalationStatuses.AddRange(
                new EscalationStatus(new EscalationStatusName("Pendiente")),
                new EscalationStatus(new EscalationStatusName("En Progreso")),
                new EscalationStatus(new EscalationStatusName("Resuelto"))
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedAdminUserAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync()) return;

            var roles = await context.Roles.ToListAsync();
            var adminRole = roles.FirstOrDefault(r => r.Name.Value == "Administrador");

            if (adminRole is null)
                throw new InvalidOperationException("No se pudo sembrar el usuario admin: falta el rol 'Administrador'.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123!");

            var admin = new User(
                roleId: adminRole.Id,
                name: new UserName("Administrador"),
                email: new Email("admin@smartinventory.com"),
                passwordHash: new PasswordHash(hashedPassword)
            );

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}