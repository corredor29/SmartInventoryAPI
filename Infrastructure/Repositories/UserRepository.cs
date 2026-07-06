using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Users;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByEmailWithRoleAsync(string email)
        {
            var users = await DbSet.Include(u => u.Role).ToListAsync();
            return users.Find(u => u.Email.Value == email.ToLowerInvariant());
        }
    }
}