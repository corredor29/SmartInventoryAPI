using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services;
using Application.DTOs.Auth;
using BCrypt.Net;

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

            var token = _tokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Name = user.Name.Value,
                Email = user.Email.Value,
                Role = user.Role.Name.Value,
            };
        }
    }
}