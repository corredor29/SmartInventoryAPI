using System.Threading.Tasks;
using Application.DTOs.Auth;

namespace Application.Contracts.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
    }
}