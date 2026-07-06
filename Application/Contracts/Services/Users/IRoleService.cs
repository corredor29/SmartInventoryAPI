using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Users.Role;

namespace Application.Contracts.Services.Users
{
    public interface IRoleService
    {
        Task<IReadOnlyList<RoleDto>> GetAllAsync();
        Task<RoleDto?> GetByIdAsync(int id);
        Task<RoleDto> CreateAsync(CreateRoleRequest request);
        Task<RoleDto?> UpdateAsync(int id, UpdateRoleRequest request);
        Task<bool> DeleteAsync(int id);
    }
}