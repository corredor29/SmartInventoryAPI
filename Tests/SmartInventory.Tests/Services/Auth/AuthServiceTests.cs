using Application.Contracts.Repositories;
using Application.Contracts.Services;
using Application.DTOs.Auth;
using Application.Services;
using Domain.Entities.Users;
using Domain.ValueObject.Users.Role;
using Domain.ValueObject.Users.User;
using Moq;
using SmartInventory.Tests.TestHelpers;

namespace SmartInventory.Tests.Services.Auth
{
    public class AuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);
            _sut = new AuthService(_unitOfWorkMock.Object, _tokenServiceMock.Object);
        }

        private static User BuildUser(string email, string plainPassword, string roleName = "Administrador")
        {
            var role = new Role(new RoleName(roleName));
            EntityReflectionHelper.SetId(role, 1);

            var user = new User(
                roleId: 1,
                name: new UserName("Ana Torres"),
                email: new Email(email),
                passwordHash: new PasswordHash(BCrypt.Net.BCrypt.HashPassword(plainPassword)));
            EntityReflectionHelper.SetId(user, 1);
            EntityReflectionHelper.SetProperty(user, nameof(User.Role), role);

            return user;
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponseWithToken()
        {
            // Arrange
            var user = BuildUser("ana@test.com", "Secret123!");
            _userRepositoryMock.Setup(r => r.GetByEmailWithRoleAsync("ana@test.com")).ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateToken(user)).Returns("fake-jwt-token");

            var request = new LoginRequestDto { Email = "ana@test.com", Password = "Secret123!" };

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.Token));
            Assert.Equal("fake-jwt-token", result.Token);
            Assert.Equal("Ana Torres", result.Name);
            Assert.Equal("ana@test.com", result.Email);
            Assert.Equal("Administrador", result.Role);
            Assert.Equal(1, result.UserId);
        }

        [Fact]
        public async Task LoginAsync_UserDoesNotExist_ReturnsNull()
        {
            // Arrange
            _userRepositoryMock.Setup(r => r.GetByEmailWithRoleAsync("missing@test.com")).ReturnsAsync((User?)null);

            var request = new LoginRequestDto { Email = "missing@test.com", Password = "whatever" };

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.Null(result);
            _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            // Arrange
            var user = BuildUser("ana@test.com", "Secret123!");
            _userRepositoryMock.Setup(r => r.GetByEmailWithRoleAsync("ana@test.com")).ReturnsAsync(user);

            var request = new LoginRequestDto { Email = "ana@test.com", Password = "WrongPassword" };

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.Null(result);
            _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }
    }
}
