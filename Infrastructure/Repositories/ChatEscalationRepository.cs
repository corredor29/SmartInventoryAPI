using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Chats;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class ChatEscalationRepository : Repository<ChatEscalation>, IChatEscalationRepository
    {
        public ChatEscalationRepository(AppDbContext context) : base(context) { }

        public override async Task<ChatEscalation?> GetByIdAsync(int id)
        {
            return await DbSet
                .Include(e => e.EscalationStatus)
                .Include(e => e.ChatSession)
                    .ThenInclude(cs => cs.Customer)
                .Include(e => e.AssignedUser)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IReadOnlyList<ChatEscalation>> GetPendingAsync()
        {
            var escalations = await DbSet
                .Include(e => e.EscalationStatus)
                .Include(e => e.ChatSession)
                    .ThenInclude(cs => cs.Customer)
                .Include(e => e.AssignedUser)
                .ToListAsync();

            return escalations
                .Where(e => e.EscalationStatus.Name.Value == "Pendiente"
                    || e.EscalationStatus.Name.Value == "En Progreso")
                .OrderByDescending(e => e.CreatedAt)
                .ToList();
        }

        public async Task<ChatEscalation?> GetByChatSessionIdAsync(int chatSessionId)
        {
            return await DbSet
                .Include(e => e.EscalationStatus)
                .Include(e => e.ChatSession)
                    .ThenInclude(cs => cs.Customer)
                .Include(e => e.AssignedUser)
                .Where(e => e.ChatSessionId == chatSessionId)
                .OrderByDescending(e => e.Id)
                .FirstOrDefaultAsync();
        }
    }
}
