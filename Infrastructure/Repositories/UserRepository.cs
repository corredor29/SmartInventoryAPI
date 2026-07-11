using System.Collections.Generic;
using System.Linq;
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

        public async Task<IReadOnlyList<User>> GetAllWithRoleAsync()
        {
            return await DbSet
                .Include(u => u.Role)
                .OrderBy(u => u.Id)
                .ToListAsync();
        }

        public async Task<User?> GetByIdWithRoleAsync(int id)
        {
            return await DbSet
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
