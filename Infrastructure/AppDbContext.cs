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
    /// <summary>
    /// Contexto principal de Entity Framework Core para la aplicación SmartInventoryAPI.
    /// Representa la sesión con la base de datos PostgreSQL y proporciona acceso
    /// a todas las entidades del dominio mediante DbSets.
    /// </summary>
    /// <remarks>
    /// Este contexto está configurado para usar PostgreSQL con la extensión pgvector,
    /// que permite almacenar y consultar embeddings vectoriales para búsqueda semántica.
    /// Las configuraciones de Fluent API se aplican automáticamente desde la carpeta Configurations/.
    /// </remarks>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Constructor que recibe las opciones de configuración del DbContext.
        /// Las opciones se inyectan desde el contenedor de dependencias en Program.cs.
        /// </summary>
        /// <param name="options">Opciones de configuración del DbContext (connection string, provider, etc.).</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ==============================================================================
        // DBSETS - ENTIDADES DEL DOMINIO
        // ==============================================================================
        // Cada DbSet representa una tabla en la base de datos y permite realizar
        // consultas LINQ, agregar, actualizar y eliminar entidades de esa tabla.

        // ------------------------------------------------------------------------------
        // ROLES Y USUARIOS
        // ------------------------------------------------------------------------------
        /// <summary>
        /// Tabla de roles del sistema (Administrador, Asesor, Cliente).
        /// Define los permisos y accesos de los usuarios en la aplicación.
        /// </summary>
        public DbSet<Role> Roles => Set<Role>();

        /// <summary>
        /// Tabla de usuarios del sistema con credenciales de autenticación.
        /// Cada usuario tiene un rol asignado y puede estar vinculado a un Customer.
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Tabla de clientes (compradores).
        /// Representa a las personas que realizan compras, independientemente de si tienen cuenta de usuario.
        /// </summary>
        public DbSet<Customer> Customers => Set<Customer>();

        // ------------------------------------------------------------------------------
        // CATÁLOGO DE PRODUCTOS
        // ------------------------------------------------------------------------------
        /// <summary>
        /// Tabla de categorías de productos (Laptops, Periféricos, Componentes, Accesorios).
        /// Sirve para organizar y filtrar productos en el catálogo.
        /// </summary>
        public DbSet<Category> Categories => Set<Category>();

        /// <summary>
        /// Tabla de estados de producto (Activo, Inactivo).
        /// Controla la visibilidad y disponibilidad de productos en el catálogo.
        /// </summary>
        public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();

        /// <summary>
        /// Tabla principal de productos.
        /// Contiene información de nombre, descripción, precio, categoría, estado,
        /// imagen y embedding vectorial para búsqueda semántica.
        /// </summary>
        public DbSet<Product> Products => Set<Product>();

        // ------------------------------------------------------------------------------
        // INVENTARIO
        // ------------------------------------------------------------------------------
        /// <summary>
        /// Tabla de inventario (stock actual por producto).
        /// Cada registro representa el stock disponible de un producto específico.
        /// </summary>
        public DbSet<Inventory> Inventories => Set<Inventory>();

        /// <summary>
        /// Tabla de tipos de movimiento de inventario (Entrada, Salida, Ajuste).
        /// Clasifica los movimientos de stock para control y auditoría.
        /// </summary>
        public DbSet<MovementType> MovementTypes => Set<MovementType>();

        /// <summary>
        /// Tabla de movimientos de inventario (historial de cambios de stock).
        /// Registra cada entrada, salida o ajuste de stock con fecha y razón.
        /// </summary>
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

        // ------------------------------------------------------------------------------
        // VENTAS Y FACTURACIÓN
        // ------------------------------------------------------------------------------
        /// <summary>
        /// Tabla de orígenes de venta (Manual, Chatbot).
        /// Indica cómo se originó la venta (manualmente por asesor o automáticamente por chatbot).
        /// </summary>
        public DbSet<SaleOrigin> SaleOrigins => Set<SaleOrigin>();

        /// <summary>
        /// Tabla de estados de venta (Pendiente, Completada, Cancelada).
        /// Controla el ciclo de vida de una venta desde su creación hasta su finalización.
        /// </summary>
        public DbSet<SaleStatus> SaleStatuses => Set<SaleStatus>();

        /// <summary>
        /// Tabla principal de ventas (encabezados de pedido).
        /// Contiene información del cliente, origen, estado, fecha y total de la venta.
        /// </summary>
        public DbSet<Sale> Sales => Set<Sale>();

        /// <summary>
        /// Tabla de detalles de venta (líneas de pedido).
        /// Contiene los productos incluidos en cada venta con cantidad y precio unitario.
        /// </summary>
        public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

        /// <summary>
        /// Tabla de facturas generadas a partir de ventas.
        /// Contiene número de factura secuencial, fecha de emisión y referencia a la venta.
        /// </summary>
        public DbSet<Invoice> Invoices => Set<Invoice>();

        // ------------------------------------------------------------------------------
        // CHATBOT Y ESCALAMIENTO
        // ------------------------------------------------------------------------------
        /// <summary>
        /// Tabla de estados de sesión de chat (Activa, Escalada, Cerrada).
        /// Controla el estado de las conversaciones con el chatbot.
        /// </summary>
        public DbSet<ChatSessionStatus> ChatSessionStatuses => Set<ChatSessionStatus>();

        /// <summary>
        /// Tabla de sesiones de chat.
        /// Representa una conversación completa entre un cliente y el chatbot,
        /// puede estar vinculada a un Customer y tiene un estado.
        /// </summary>
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();

        /// <summary>
        /// Tabla de tipos de remitente de mensajes (Bot, Cliente, Asesor).
        /// Clasifica quién envió cada mensaje en una sesión de chat.
        /// </summary>
        public DbSet<SenderType> SenderTypes => Set<SenderType>();

        /// <summary>
        /// Tabla de mensajes de chat.
        /// Contiene todos los mensajes intercambiados en las sesiones de chat
        /// con contenido, remitente y timestamp.
        /// </summary>
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        /// <summary>
        /// Tabla de estados de escalamiento (Pendiente, En Progreso, Resuelto).
        /// Controla el estado de las solicitudes de atención humana.
        /// </summary>
        public DbSet<EscalationStatus> EscalationStatuses => Set<EscalationStatus>();

        /// <summary>
        /// Tabla de escalamientos de chat a asesores humanos.
        /// Se crea cuando el chatbot no puede resolver una consulta y necesita intervención humana.
        /// </summary>
        public DbSet<ChatEscalation> ChatEscalations => Set<ChatEscalation>();

        // ==============================================================================
        // CONFIGURACIÓN DEL MODELO (ONMODELCREATING)
        // ==============================================================================
        /// <summary>
        /// Configura el modelo de Entity Framework Core cuando se crea el contexto.
        /// Aquí se definen las configuraciones de Fluent API y extensiones de PostgreSQL.
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo de EF Core.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Habilita la extensión "vector" de PostgreSQL para pgvector
            // Esta extensión permite crear columnas de tipo vector para almacenar embeddings
            // necesarios para la búsqueda semántica de productos con OpenAI
            modelBuilder.HasPostgresExtension("vector");

            // Aplica automáticamente todas las configuraciones de Fluent API
            // que implementen IEntityTypeConfiguration<T> y estén en este ensamblado.
            // Esto permite mantener cada configuración en su propio archivo en la carpeta Configurations/
            // (ej: ProductConfiguration.cs, UserConfiguration.cs, etc.)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Llama a la implementación base para aplicar configuraciones adicionales
            base.OnModelCreating(modelBuilder);
        }
    }
}
