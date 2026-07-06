using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Users;
using Application.DTOs.Users.Role;
using Domain.Entities.Users;
using Domain.ValueObject.Users.Role;

namespace Application.Services.Users
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
        {
            var roles = await _unitOfWork.Repository<Role>().GetAllAsync();
            return roles.Select(ToDto).ToList();
        }

        public async Task<RoleDto?> GetByIdAsync(int id)
        {
            var role = await _unitOfWork.Repository<Role>().GetByIdAsync(id);
            return role is null ? null : ToDto(role);
        }

        public async Task<RoleDto> CreateAsync(CreateRoleRequest request)
        {
            var role = new Role(new RoleName(request.Name));

            await _unitOfWork.Repository<Role>().AddAsync(role);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(role);
        }

        public async Task<RoleDto?> UpdateAsync(int id, UpdateRoleRequest request)
        {
            var role = await _unitOfWork.Repository<Role>().GetByIdAsync(id);
            if (role is null) return null;

            role.Update(new RoleName(request.Name));
            _unitOfWork.Repository<Role>().Update(role);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(role);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _unitOfWork.Repository<Role>().GetByIdAsync(id);
            if (role is null) return false;

            _unitOfWork.Repository<Role>().Remove(role);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static RoleDto ToDto(Role role) => new()
        {
            RoleId = role.Id,
            Name = role.Name.Value,
        };
    }
}