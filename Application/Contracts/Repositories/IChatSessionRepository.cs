using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Chats;

namespace Application.Contracts.Repositories
{
    public interface IChatSessionRepository : IRepository<ChatSession>
    {

        Task<ChatSession?> GetByIdWithMessagesAsync(int chatSessionId);
    }
}