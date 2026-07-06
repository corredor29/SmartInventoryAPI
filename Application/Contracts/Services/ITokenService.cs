using Domain.Entities.Users;

namespace Application.Contracts.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}