using Microsoft.EntityFrameworkCore;
using Domain.Entities.Users;
using Domain.Entities.Customers;
using Domain.Entities.Products;
using Domain.Entities.Inventories;
using Domain.Entities.Sales;
using Domain.Entities.Invoices;
using Domain.Entities.Chats;

namespace Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Roles y usuarios
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Customer> Customers => Set<Customer>();

        // Catálogo de productos
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();
        public DbSet<Product> Products => Set<Product>();

        // Inventario
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<MovementType> MovementTypes => Set<MovementType>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

        // Ventas y facturación
        public DbSet<SaleOrigin> SaleOrigins => Set<SaleOrigin>();
        public DbSet<SaleStatus> SaleStatuses => Set<SaleStatus>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
        public DbSet<Invoice> Invoices => Set<Invoice>();

        // Chatbot y escalamiento
        public DbSet<ChatSessionStatus> ChatSessionStatuses => Set<ChatSessionStatus>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<SenderType> SenderTypes => Set<SenderType>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<EscalationStatus> EscalationStatuses => Set<EscalationStatus>();
        public DbSet<ChatEscalation> ChatEscalations => Set<ChatEscalation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            // Aplica automáticamente todas las clases IEntityTypeConfiguration<T>
            // que estén en este mismo ensamblado (carpeta Configurations/)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
