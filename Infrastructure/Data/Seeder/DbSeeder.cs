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
using Domain.ValueObject.Products.Product;
using Domain.ValueObject.Inventories.Inventory;
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
            await SeedDefaultUsersAsync(context);
            await SeedCatalogProductsAsync(context);
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

        private static async Task SeedDefaultUsersAsync(AppDbContext context)
        {
            var roles = await context.Roles.ToListAsync();
            var adminRole = roles.FirstOrDefault(r => r.Name.Value == "Administrador");
            var asesorRole = roles.FirstOrDefault(r => r.Name.Value == "Asesor");

            if (adminRole is null)
                throw new InvalidOperationException("No se pudo sembrar usuarios: falta el rol 'Administrador'.");
            if (asesorRole is null)
                throw new InvalidOperationException("No se pudo sembrar usuarios: falta el rol 'Asesor'.");

            var users = await context.Users.ToListAsync();
            var added = false;

            if (!users.Any(u => string.Equals(u.Email.Value, "admin@smartinventory.com", StringComparison.OrdinalIgnoreCase)))
            {
                context.Users.Add(new User(
                    roleId: adminRole.Id,
                    name: new UserName("Administrador"),
                    email: new Email("admin@smartinventory.com"),
                    passwordHash: new PasswordHash(BCrypt.Net.BCrypt.HashPassword("Admin123!"))
                ));
                added = true;
            }

            if (!users.Any(u => string.Equals(u.Email.Value, "danny.velasco@smartinventory.com", StringComparison.OrdinalIgnoreCase)))
            {
                context.Users.Add(new User(
                    roleId: asesorRole.Id,
                    name: new UserName("Danny Velasco"),
                    email: new Email("danny.velasco@smartinventory.com"),
                    passwordHash: new PasswordHash(BCrypt.Net.BCrypt.HashPassword("Asesor123!"))
                ));
                added = true;
            }

            if (!users.Any(u => string.Equals(u.Email.Value, "andres.navas@smartinventory.com", StringComparison.OrdinalIgnoreCase)))
            {
                context.Users.Add(new User(
                    roleId: asesorRole.Id,
                    name: new UserName("Andres Navas"),
                    email: new Email("andres.navas@smartinventory.com"),
                    passwordHash: new PasswordHash(BCrypt.Net.BCrypt.HashPassword("Asesor123!"))
                ));
                added = true;
            }

            if (added)
                await context.SaveChangesAsync();
        }

        /// <summary>
        /// Catálogo demo (~43 productos) para pruebas de chatbot, búsqueda semántica y dashboard.
        /// Idempotente: solo inserta nombres que aún no existan.
        /// </summary>
        private static async Task SeedCatalogProductsAsync(AppDbContext context)
        {
            var categories = await context.Categories.ToListAsync();
            var activeStatus = (await context.ProductStatuses.ToListAsync())
                .FirstOrDefault(s => string.Equals(s.Name.Value, "Activo", StringComparison.OrdinalIgnoreCase));

            if (activeStatus is null || categories.Count == 0)
                return;

            int Cat(string name) =>
                categories.First(c => string.Equals(c.Name.Value, name, StringComparison.OrdinalIgnoreCase)).Id;

            var catalog = new (string Category, string Name, string Description, decimal Price, int Stock)[]
            {
                // Laptops (11)
                ("Laptops", "Laptop HP Pavilion 15", "Intel i5, 16GB RAM, 512GB SSD", 2500000m, 8),
                ("Laptops", "Lenovo LOQ 15", "Portátil gamer RTX 4050, 16GB RAM, 512GB SSD", 3500000m, 9),
                ("Laptops", "ASUS VivoBook 14", "Ultraliviana AMD Ryzen 5, 8GB RAM, 512GB SSD", 1890000m, 12),
                ("Laptops", "Dell Inspiron 15", "Intel i7, 16GB RAM, 1TB SSD, pantalla Full HD", 3200000m, 6),
                ("Laptops", "Acer Aspire 5", "Laptop oficina Intel i5, 8GB RAM, 256GB SSD", 1650000m, 15),
                ("Laptops", "MacBook Air M2", "Apple Silicon M2, 8GB unificados, 256GB SSD", 5200000m, 4),
                ("Laptops", "HP Victus 16", "Gamer Intel i5, RTX 4060, 16GB RAM, 512GB SSD", 4100000m, 5),
                ("Laptops", "Lenovo IdeaPad Slim 3", "Estudiantil AMD Ryzen 3, 8GB RAM, 256GB SSD", 1290000m, 18),
                ("Laptops", "ASUS TUF Gaming A15", "Gamer AMD Ryzen 7, RTX 4050, 16GB RAM, 512GB SSD", 4290000m, 7),
                ("Laptops", "Acer Nitro 5", "Gamer Intel i5, RTX 4050, 16GB RAM, 512GB SSD", 3890000m, 8),
                ("Laptops", "Lenovo Legion 5", "Gamer AMD Ryzen 7, RTX 4060, 16GB RAM, 1TB SSD", 4990000m, 5),

                // Periféricos (12)
                ("Periféricos", "Teclado mecánico Redragon Kumara", "Switch Outemu Blue, retroiluminado RGB", 189000m, 25),
                ("Periféricos", "Teclado Logitech MX Keys", "Inalámbrico silencioso para oficina", 520000m, 10),
                ("Periféricos", "Mouse Logitech G502 Hero", "Gamer cableado, 25600 DPI, pesos ajustables", 245000m, 20),
                ("Periféricos", "Mouse inalámbrico Logitech M280", "Ergonómico para oficina, receptor USB", 89000m, 40),
                ("Periféricos", "Audífonos Sony WH-CH720N", "Bluetooth con cancelación de ruido", 680000m, 14),
                ("Periféricos", "Headset HyperX Cloud Stinger 2", "Diadema gamer con micrófono", 210000m, 22),
                ("Periféricos", "Audífonos Bluetooth JBL Tune 510BT", "On-ear ligeros con batería de larga duración", 199000m, 30),
                ("Periféricos", "Webcam Logitech C920s", "Full HD 1080p con micrófono estéreo", 380000m, 11),
                ("Periféricos", "Micrófono Blue Yeti Nano", "USB condensador para streaming y llamadas", 450000m, 7),
                ("Periféricos", "Monitor Samsung 24 Full HD", "IPS 75Hz, HDMI y VGA", 620000m, 9),
                ("Periféricos", "Monitor LG UltraGear 27", "144Hz 1ms, ideal para gaming", 1150000m, 6),
                ("Periféricos", "Pad mouse XL RGB", "Superficie extendida antideslizante", 65000m, 35),

                // Componentes (10)
                ("Componentes", "SSD NVMe Kingston 1TB", "PCIe 4.0 lectura hasta 7000 MB/s", 420000m, 16),
                ("Componentes", "SSD SATA Crucial 480GB", "2.5 pulgadas para upgrade de laptop/PC", 185000m, 20),
                ("Componentes", "Memoria RAM DDR4 16GB Kingston", "3200MHz CL16 módulo único", 230000m, 28),
                ("Componentes", "Memoria RAM DDR5 32GB Corsair", "Kit 2x16GB 5600MHz", 780000m, 8),
                ("Componentes", "Fuente EVGA 650W 80 Plus Gold", "Modular, certificada para PC gamer", 390000m, 10),
                ("Componentes", "Cooler CPU Cooler Master Hyper 212", "Disipador torre con ventilador 120mm", 165000m, 12),
                ("Componentes", "Tarjeta gráfica RTX 4060 8GB", "NVIDIA Ada Lovelace, DLSS 3", 2100000m, 3),
                ("Componentes", "Placa madre B550M Gigabyte", "AMD AM4, WiFi, mATX", 540000m, 7),
                ("Componentes", "Disco HDD Seagate 2TB", "7200RPM SATA para almacenamiento masivo", 275000m, 15),
                ("Componentes", "Pasta térmica Arctic MX-4", "Alta conductividad 4g", 45000m, 50),

                // Accesorios (10)
                ("Accesorios", "Mochila laptop HP Prelude 15.6", "Compartimento acolchado antirrobo", 145000m, 18),
                ("Accesorios", "Cargador universal laptop 90W", "Compatible HP/Dell/Lenovo con tips intercambiables", 120000m, 24),
                ("Accesorios", "Hub USB-C 7 en 1", "HDMI 4K, USB 3.0, lector SD y PD", 159000m, 16),
                ("Accesorios", "Cable HDMI 2.1 2 metros", "Soporta 4K 120Hz y 8K", 45000m, 40),
                ("Accesorios", "Base refrigerante laptop Cooler Master", "5 ventiladores y altura ajustable", 98000m, 13),
                ("Accesorios", "Soporte monitor brazo articulado", "VESA hasta 32 pulgadas", 210000m, 8),
                ("Accesorios", "Funda tablet/laptop 14 neopreno", "Protección ligera contra golpes", 55000m, 22),
                ("Accesorios", "Filtro de privacidad laptop 15.6", "Ángulo estrecho anti-mirones", 89000m, 10),
                ("Accesorios", "Limpieza kit pantallas 3 en 1", "Spray, paño microfibra y brocha", 35000m, 45),
                ("Accesorios", "UPS APC 600VA", "Respaldo eléctrico para PC y router", 320000m, 2),
            };

            var existingNames = (await context.Products.ToListAsync())
                .Select(p => p.Name.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var created = new List<Product>();
            foreach (var item in catalog)
            {
                if (existingNames.Contains(item.Name))
                    continue;

                var product = new Product(
                    categoryId: Cat(item.Category),
                    productStatusId: activeStatus.Id,
                    name: new ProductName(item.Name),
                    price: new ProductPrice(item.Price),
                    description: new ProductDescription(item.Description)
                );
                context.Products.Add(product);
                created.Add(product);
                existingNames.Add(item.Name);
            }

            if (created.Count == 0)
                return;

            await context.SaveChangesAsync();

            foreach (var product in created)
            {
                var stock = catalog.First(c =>
                    string.Equals(c.Name, product.Name.Value, StringComparison.OrdinalIgnoreCase)).Stock;
                context.Inventories.Add(new Inventory(product.Id, new StockQuantity(stock)));
            }

            await context.SaveChangesAsync();
        }
    }
}