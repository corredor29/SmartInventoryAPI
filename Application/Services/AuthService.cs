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
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(request.Email);

            if (user is null)
                return null;

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash.Value);

            if (!isPasswordValid)
                return null;

            // Cliente: asegurar vínculo al Customer del mismo email (corrige vínculos viejos/incorrectos)
            if (string.Equals(user.Role.Name.Value, "Cliente", System.StringComparison.OrdinalIgnoreCase))
            {
                await EnsureCustomerLinkedAsync(user);
            }

            var token = _tokenService.GenerateToken(user);

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

        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request)
        {
            var existing = await _unitOfWork.Users.GetByEmailWithRoleAsync(request.Email);
            if (existing is not null)
                return null;

            var roles = await _unitOfWork.Repository<Role>().GetAllAsync();
            var clientRole = roles.FirstOrDefault(r =>
                string.Equals(r.Name.Value, "Cliente", System.StringComparison.OrdinalIgnoreCase));

            if (clientRole is null)
                throw new System.InvalidOperationException("No se encontró el rol 'Cliente' en el catálogo.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var customer = new Customer(
                new CustomerName(request.Name),
                new CustomerEmail(request.Email));
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            var user = new User(
                roleId: clientRole.Id,
                name: new UserName(request.Name),
                email: new Email(request.Email),
                passwordHash: new PasswordHash(hashedPassword),
                customerId: customer.Id
            );

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);

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

        private async Task EnsureCustomerLinkedAsync(User user)
        {
            // Reutilizar Customer existente con el mismo email (evita pedidos huérfanos).
            var existing = await _unitOfWork.Customers.GetByEmailAsync(user.Email.Value);
            if (existing is null)
            {
                existing = new Customer(
                    new CustomerName(user.Name.Value),
                    new CustomerEmail(user.Email.Value));
                await _unitOfWork.Customers.AddAsync(existing);
                await _unitOfWork.SaveChangesAsync();
            }

            if (user.CustomerId == existing.Id)
                return;

            user.LinkCustomer(existing.Id);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
