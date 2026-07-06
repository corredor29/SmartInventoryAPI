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

        public async Task<IReadOnlyList<ChatEscalation>> GetPendingAsync()
        {
            var escalations = await DbSet
                .Include(e => e.EscalationStatus)
                .Include(e => e.ChatSession)
                .Include(e => e.AssignedUser)
                .ToListAsync();

            return escalations
                .Where(e => e.EscalationStatus.Name.Value == "Pendiente")
                .ToList();
        }

        public async Task<ChatEscalation?> GetByChatSessionIdAsync(int chatSessionId)
        {
            return await DbSet.FirstOrDefaultAsync(e => e.ChatSessionId == chatSessionId);
        }
    }
}