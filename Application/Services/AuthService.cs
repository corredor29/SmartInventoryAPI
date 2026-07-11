using System.Threading.Tasks;
using System.Linq;
using Application.Contracts.Repositories;
using Application.Contracts.Services;
using Application.DTOs.Auth;
using Domain.Entities.Customers;
using Domain.Entities.Users;
using Domain.ValueObject.Customers.Customer;
using Domain.ValueObject.Users.User;

namespace Application.Services
{
    /// <summary>
    /// Servicio de autenticación que contiene la lógica de negocio para login y registro de usuarios.
    /// Maneja validación de credenciales, vinculación de usuarios con clientes y generación de tokens JWT.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Constructor que inyecta las dependencias necesarias mediante inyección de dependencias.
        /// </summary>
        /// <param name="unitOfWork">Unit of Work para acceso a datos y transacciones.</param>
        /// <param name="tokenService">Servicio para generar tokens JWT.</param>
        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Autentica un usuario existente validando email y contraseña.
        /// Si las credenciales son válidas, genera un token JWT y asegura la vinculación con Customer.
        /// </summary>
        /// <param name="request">DTO con email y contraseña del usuario.</param>
        /// <returns>
        /// AuthResponseDto con token JWT y datos del usuario si las credenciales son válidas.
        /// Null si el email no existe o la contraseña es incorrecta.
        /// </returns>
        /// <remarks>
        /// Para usuarios con rol "Cliente", este método asegura que estén vinculados
        /// a un Customer con el mismo email para evitar pedidos huérfanos.
        /// </remarks>
        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            // Busca el usuario por email incluyendo su rol (necesario para verificar el rol)
            var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(request.Email);

            // Si no existe un usuario con ese email, retorna null (credenciales inválidas)
            if (user is null)
                return null;

            // Verifica la contraseña usando BCrypt (compara el hash almacenado con la contraseña ingresada)
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash.Value);

            // Si la contraseña no coincide, retorna null (credenciales inválidas)
            if (!isPasswordValid)
                return null;

            // ==============================================================================
            // VINCULACIÓN DE CUSTOME (SOLO PARA ROL CLIENTE)
            // ==============================================================================
            // Si el usuario tiene rol Cliente, asegura que esté vinculado a un Customer
            // con el mismo email. Esto corrige vínculos viejos o incorrectos y evita
            // que los pedidos queden huérfanos sin Customer asociado.
            if (string.Equals(user.Role.Name.Value, "Cliente", System.StringComparison.OrdinalIgnoreCase))
            {
                await EnsureCustomerLinkedAsync(user);
            }

            // Genera un token JWT con los claims del usuario (userId, customerId, role, name, email)
            var token = _tokenService.GenerateToken(user);

            // Retorna el DTO de respuesta con el token y los datos del usuario
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                CustomerId = user.CustomerId,
                Name = user.Name.Value,
                Email = user.Email.Value,
                Role = user.Role.Name.Value,
            };
        }

        /// <summary>
        /// Registra un nuevo usuario con rol Cliente por defecto.
        /// Crea automáticamente una entidad Customer vinculada y genera un token JWT.
        /// </summary>
        /// <param name="request">DTO con nombre, email y contraseña del nuevo usuario.</param>
        /// <returns>
        /// AuthResponseDto con token JWT y datos del usuario si el registro es exitoso.
        /// Null si ya existe un usuario con el mismo email.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no existe el rol "Cliente" en el catálogo de roles.
        /// </exception>
        /// <remarks>
        /// Este método realiza las siguientes operaciones en transacción:
        /// 1. Verifica que no exista un usuario con el mismo email
        /// 2. Busca el rol "Cliente" en el catálogo
        /// 3. Hashea la contraseña con BCrypt
        /// 4. Crea una entidad Customer
        /// 5. Crea una entidad User vinculada al Customer
        /// 6. Genera un token JWT
        /// </remarks>
        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request)
        {
            // Verifica si ya existe un usuario con el mismo email
            var existing = await _unitOfWork.Users.GetByEmailWithRoleAsync(request.Email);
            if (existing is not null)
                return null; // Email ya registrado

            // Busca el rol "Cliente" en el catálogo de roles
            var roles = await _unitOfWork.Repository<Role>().GetAllAsync();
            var clientRole = roles.FirstOrDefault(r =>
                string.Equals(r.Name.Value, "Cliente", System.StringComparison.OrdinalIgnoreCase));

            // Si no existe el rol Cliente, lanza excepción (error de configuración del seeder)
            if (clientRole is null)
                throw new System.InvalidOperationException("No se encontró el rol 'Cliente' en el catálogo.");

            // ==============================================================================
            // CREACIÓN DE CUSTOMER
            // ==============================================================================
            // Hashea la contraseña con BCrypt (factor de costo por defecto: 10)
            // BCrypt agrega salt automáticamente para proteger contra ataques rainbow table
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Crea la entidad Customer usando Value Objects para validación de dominio
            var customer = new Customer(
                new CustomerName(request.Name),
                new CustomerEmail(request.Email));
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync(); // Guarda Customer para obtener su ID

            // ==============================================================================
            // CREACIÓN DE USUARIO
            // ==============================================================================
            // Crea la entidad User vinculada al Customer creado anteriormente
            var user = new User(
                roleId: clientRole.Id,
                name: new UserName(request.Name),
                email: new Email(request.Email),
                passwordHash: new PasswordHash(hashedPassword),
                customerId: customer.Id // Vincula el User al Customer recién creado
            );

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(); // Guarda User

            // ==============================================================================
            // GENERACIÓN DE TOKEN JWT
            // ==============================================================================
            // Genera un token JWT con los claims del usuario recién creado
            var token = _tokenService.GenerateToken(user);

            // Retorna el DTO de respuesta con el token y los datos del usuario
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                CustomerId = user.CustomerId,
                Name = user.Name.Value,
                Email = user.Email.Value,
                Role = clientRole.Name.Value,
            };
        }

        /// <summary>
        /// Método privado que asegura que un usuario con rol Cliente esté vinculado a un Customer.
        /// Reutiliza un Customer existente con el mismo email o crea uno nuevo si no existe.
        /// </summary>
        /// <param name="user">Usuario que debe ser vinculado a un Customer.</param>
        /// <remarks>
        /// Este método es importante para mantener la integridad referencial:
        /// - Evita que los pedidos queden huérfanos sin Customer asociado
        /// - Corrige vínculos viejos o incorrectos (ej: si el Customer fue recreado)
        /// - Permite que usuarios creados manualmente (sin Customer) se vinculen automáticamente
        /// </remarks>
        private async Task EnsureCustomerLinkedAsync(User user)
        {
            // Busca si ya existe un Customer con el mismo email que el usuario
            var existing = await _unitOfWork.Customers.GetByEmailAsync(user.Email.Value);

            // Si no existe un Customer con ese email, crea uno nuevo
            if (existing is null)
            {
                existing = new Customer(
                    new CustomerName(user.Name.Value),
                    new CustomerEmail(user.Email.Value));
                await _unitOfWork.Customers.AddAsync(existing);
                await _unitOfWork.SaveChangesAsync();
            }

            // Si el usuario ya está vinculado al Customer correcto, no hace nada
            if (user.CustomerId == existing.Id)
                return;

            // Vincula el usuario al Customer encontrado o creado
            user.LinkCustomer(existing.Id);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
