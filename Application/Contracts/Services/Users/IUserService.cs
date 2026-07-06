using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Users.User;

namespace Application.Contracts.Services.Users
{
    public interface IUserService
    {
        Task<IReadOnlyList<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserRequest request);
        Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request);
        Task<bool> DeleteAsync(int id);
    }
}