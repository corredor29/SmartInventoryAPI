using System.Threading.Tasks;
using Domain.Entities.Users;

namespace Application.Contracts.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailWithRoleAsync(string email);
    }
}