using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Chats;

namespace Application.Contracts.Repositories
{
    public interface IChatEscalationRepository : IRepository<ChatEscalation>
    {

        Task<IReadOnlyList<ChatEscalation>> GetPendingAsync();

        Task<ChatEscalation?> GetByChatSessionIdAsync(int chatSessionId);
    }
}