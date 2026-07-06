using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatSession;
using Domain.Entities.Chats;

namespace Application.Services.Chats
{
    public class ChatSessionService : IChatSessionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatSessionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ChatSessionDto>> GetAllAsync()
        {
            var sessions = await _unitOfWork.ChatSessions.GetAllAsync();
            return sessions.Select(ToDto).ToList();
        }

        public async Task<ChatSessionDto?> GetByIdAsync(int id)
        {
            var session = await _unitOfWork.ChatSessions.GetByIdWithMessagesAsync(id);
            return session is null ? null : ToDto(session);
        }

        public async Task<ChatSessionDto> CreateAsync(CreateChatSessionRequest request)
        {
            // Estado inicial "Activa" — se busca por nombre para no depender de un Id fijo
            var activeStatuses = await _unitOfWork.Repository<ChatSessionStatus>().GetAllAsync();
            var activeStatus = activeStatuses.FirstOrDefault(s =>
                string.Equals(s.Name.Value, "Activa", System.StringComparison.OrdinalIgnoreCase));

            if (activeStatus is null)
                throw new System.InvalidOperationException("No se encontró el estado 'Activa' en el catálogo ChatSessionStatus.");

            var session = new ChatSession(activeStatus.Id, request.CustomerId);

            await _unitOfWork.ChatSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(session);
        }

        public async Task<ChatSessionDto?> ChangeStatusAsync(int id, int chatSessionStatusId)
        {
            var session = await _unitOfWork.ChatSessions.GetByIdAsync(id);
            if (session is null) return null;

            session.ChangeStatus(chatSessionStatusId);
            _unitOfWork.ChatSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        private static ChatSessionDto ToDto(ChatSession session) => new()
        {
            ChatSessionId = session.Id,
            CustomerId = session.CustomerId,
            StatusName = session.ChatSessionStatus?.Name.Value ?? string.Empty,
            StartedAt = session.StartedAt.Value,
        };
    }
}