using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Customers;
using Domain.Entities.Chats;
using Domain.ValueObject.Users.User;
namespace Domain.Entities.Users
{
    /// <summary>
    /// Entidad que representa un usuario del sistema con credenciales de autenticación.
    /// Contiene información de rol, nombre, email, contraseña hasheada
    /// y vinculación opcional a un Customer.
    /// </summary>
    /// <remarks>
    /// Esta entidad:
    /// - Usa Value Objects para validación de dominio (UserName, Email, PasswordHash)
    /// - Tiene una relación muchos-a-uno con Role
    /// - Tiene una relación muchos-a-uno opcional con Customer (para usuarios con rol Cliente)
    /// - Tiene una relación uno-a-muchos con ChatEscalation (asesores asignados a escalamientos)
    /// - Es sealed para prevenir herencia y mantener la integridad del dominio
    /// - Almacena la contraseña hasheada con BCrypt (nunca en texto plano)
    /// </remarks>
    public sealed class User : BaseEntity
    {
        // ==============================================================================
        // PROPIEDADES - DATOS DEL USUARIO
        // ==============================================================================
        /// <summary>ID del rol del usuario (FK).</summary>
        public int      RoleId       { get; private set; }

        /// <summary>ID del Customer vinculado (opcional, FK).</summary>
        public int?     CustomerId   { get; private set; }

        /// <summary>Nombre del usuario (Value Object con validación).</summary>
        public UserName Name         { get; private set; } = null!;

        /// <summary>Email del usuario (Value Object con validación, usado como login).</summary>
        public Email    Email        { get; private set; } = null!;

        /// <summary>Hash de la contraseña (Value Object, generado con BCrypt).</summary>
        public PasswordHash PasswordHash { get; private set; } = null!;

        // ==============================================================================
        // PROPIEDADES DE NAVEGACIÓN - RELACIONES
        // ==============================================================================
        /// <summary>Rol del usuario (relación de navegación).</summary>
        public Role      Role     { get; private set; } = null!;

        /// <summary>Customer vinculado al usuario (opcional, relación de navegación).</summary>
        public Customer?  Customer { get; private set; }

        /// <summary>
        /// Escalamientos de chat asignados a este usuario (para asesores).
        /// Solo usuarios con rol Asesor o Administrador tienen escalamientos asignados.
        /// </summary>
        public ICollection<ChatEscalation> AssignedEscalations { get; private set; } = new List<ChatEscalation>();

        // ==============================================================================
        // CONSTRUCTOR PRIVADO PARA EF CORE
        // ==============================================================================
        /// <summary>
        /// Constructor privado requerido por Entity Framework Core.
        /// No debe usarse directamente para creación de usuarios.
        /// </summary>
        private User() { }

        // ==============================================================================
        // CONSTRUCTOR PÚBLICO - CREACIÓN DE USUARIO
        // ==============================================================================
        /// <summary>
        /// Constructor público para crear un nuevo usuario.
        /// Valida todos los parámetros usando Value Objects.
        /// </summary>
        /// <param name="roleId">ID del rol (debe ser mayor a 0).</param>
        /// <param name="name">Nombre del usuario (Value Object).</param>
        /// <param name="email">Email del usuario (Value Object).</param>
        /// <param name="passwordHash">Hash de la contraseña (Value Object, generado con BCrypt).</param>
        /// <param name="customerId">ID del Customer vinculado (opcional).</param>
        /// <exception cref="ArgumentException">Se lanza si roleId no es válido.</exception>
        /// <exception cref="ArgumentNullException">Se lanza si name, email o passwordHash son null.</exception>
        public User(int roleId, UserName name, Email email, PasswordHash passwordHash, int? customerId = null)
        {
            // Valida que el ID sea positivo
            RoleId       = roleId > 0 ? roleId : throw new ArgumentException("RoleId must be greater than 0.");
            // Valida que los Value Objects no sean null
            Name         = name         ?? throw new ArgumentNullException(nameof(name));
            Email        = email        ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            // CustomerId es opcional
            CustomerId   = customerId;
        }

        // ==============================================================================
        // MÉTODOS DE DOMINIO
        // ==============================================================================
        /// <summary>
        /// Actualiza el nombre y email del usuario.
        /// </summary>
        /// <param name="name">Nuevo nombre del usuario (Value Object).</param>
        /// <param name="email">Nuevo email del usuario (Value Object).</param>
        /// <exception cref="ArgumentNullException">Se lanza si name o email son null.</exception>
        public void Update(UserName name, Email email)
        {
            Name  = name  ?? throw new ArgumentNullException(nameof(name));
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        /// <summary>
        /// Cambia la contraseña del usuario.
        /// </summary>
        /// <param name="passwordHash">Nuevo hash de contraseña (Value Object, generado con BCrypt).</param>
        /// <exception cref="ArgumentNullException">Se lanza si passwordHash es null.</exception>
        public void ChangePassword(PasswordHash passwordHash)
        {
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        }

        /// <summary>
        /// Cambia el rol del usuario.
        /// </summary>
        /// <param name="roleId">Nuevo ID de rol (debe ser mayor a 0).</param>
        /// <exception cref="ArgumentException">Se lanza si roleId no es válido.</exception>
        public void ChangeRole(int roleId)
        {
            RoleId = roleId > 0 ? roleId : throw new ArgumentException("RoleId must be greater than 0.");
        }

        /// <summary>
        /// Vincula el usuario a un Customer.
        /// Se usa para vincular usuarios con rol Cliente a su entidad Customer correspondiente.
        /// </summary>
        /// <param name="customerId">ID del Customer a vincular (debe ser mayor a 0).</param>
        /// <exception cref="ArgumentException">Se lanza si customerId no es válido.</exception>
        public void LinkCustomer(int customerId)
        {
            CustomerId = customerId > 0 ? customerId : throw new ArgumentException("CustomerId must be greater than 0.");
        }
    }
}