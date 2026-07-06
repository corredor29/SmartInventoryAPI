using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Users;
using Application.DTOs.Users.User;
using Domain.Entities.Users;
using Domain.ValueObject.Users.User;

namespace Application.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<UserDto>> GetAllAsync()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            return users.Select(ToDto).ToList();
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            return user is null ? null : ToDto(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserRequest request)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User(
                roleId: request.RoleId,
                name: new UserName(request.Name),
                email: new Email(request.Email),
                passwordHash: new PasswordHash(hashedPassword),
                customerId: request.CustomerId
            );

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(user);
        }

        public async Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user is null) return null;

            user.Update(new UserName(request.Name), new Email(request.Email));
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(user);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user is null) return false;

            _unitOfWork.Users.Remove(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static UserDto ToDto(User user) => new()
        {
            UserId = user.Id,
            Name = user.Name.Value,
            Email = user.Email.Value,
            RoleName = user.Role?.Name.Value ?? string.Empty,
            CustomerId = user.CustomerId,
        };
    }
}