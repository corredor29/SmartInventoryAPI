using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using Domain.Entities.Chats;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class ChatSessionRepository : Repository<ChatSession>, IChatSessionRepository
    {
        public ChatSessionRepository(AppDbContext context) : base(context) { }

        public async Task<ChatSession?> GetByIdWithMessagesAsync(int chatSessionId)
        {
            return await DbSet
                .Include(cs => cs.Messages)
                .Include(cs => cs.Escalation)
                .Include(cs => cs.ChatSessionStatus)
                .FirstOrDefaultAsync(cs => cs.Id == chatSessionId);
        }
    }
}