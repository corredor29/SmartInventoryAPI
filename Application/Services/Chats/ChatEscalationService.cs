using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatEscalation;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.ChatEscalation;

namespace Application.Services.Chats
{
    public class ChatEscalationService : IChatEscalationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatEscalationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ChatEscalationDto>> GetPendingAsync()
        {
            var escalations = await _unitOfWork.ChatEscalations.GetPendingAsync();
            return escalations.Select(ToDto).ToList();
        }

        public async Task<ChatEscalationDto?> GetByIdAsync(int id)
        {
            var escalation = await _unitOfWork.ChatEscalations.GetByIdAsync(id);
            return escalation is null ? null : ToDto(escalation);
        }

        public async Task<(ChatEscalationDto Escalation, bool Created)> CreateAsync(CreateChatEscalationRequest request)
        {
            var statuses = await _unitOfWork.Repository<EscalationStatus>().GetAllAsync();
            var pendingStatus = statuses.FirstOrDefault(s =>
                string.Equals(s.Name.Value, "Pendiente", System.StringComparison.OrdinalIgnoreCase));

            if (pendingStatus is null)
                throw new System.InvalidOperationException("No se encontró el estado 'Pendiente' en EscalationStatus.");

            var chatSessionId = int.Parse(request.SessionId);

            var session = await _unitOfWork.ChatSessions.GetByIdWithMessagesAsync(chatSessionId)
                ?? await _unitOfWork.ChatSessions.GetByIdAsync(chatSessionId);

            if (session is null)
                throw new System.InvalidOperationException("La sesión de chat no existe.");

            if (session.CustomerId is null)
                throw new System.InvalidOperationException(
                    "Debes iniciar sesión para hablar con un asesor. Regístrate o inicia sesión e inténtalo de nuevo.");

            var existing = await _unitOfWork.ChatEscalations.GetByChatSessionIdAsync(chatSessionId);
            if (existing is not null && existing.ResolvedAt is null)
            {
                return (ToDto(existing), false);
            }

            var escalation = new ChatEscalation(
                chatSessionId,
                pendingStatus.Id,
                new EscalationReason(request.Reason)
            );

            await _unitOfWork.ChatEscalations.AddAsync(escalation);

            await SetSessionStatusAsync(chatSessionId, "Escalada");

            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.ChatEscalations.GetByIdAsync(escalation.Id);
            return (ToDto(created ?? escalation), true);
        }

        public async Task<ChatEscalationDto?> AssignAsync(int id, AssignChatEscalationRequest request)
        {
            var escalation = await _unitOfWork.ChatEscalations.GetByIdAsync(id);
            if (escalation is null) return null;

            var statuses = await _unitOfWork.Repository<EscalationStatus>().GetAllAsync();
            var inProgress = statuses.FirstOrDefault(s =>
                string.Equals(s.Name.Value, "En Progreso", System.StringComparison.OrdinalIgnoreCase));

            escalation.AssignTo(request.UserId);
            if (inProgress is not null)
                escalation.ChangeStatus(inProgress.Id);

            _unitOfWork.ChatEscalations.Update(escalation);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<ChatEscalationDto?> ResolveAsync(int id)
        {
            var escalation = await _unitOfWork.ChatEscalations.GetByIdAsync(id);
            if (escalation is null) return null;

            if (escalation.ResolvedAt is not null)
                return ToDto(escalation);

            var statuses = await _unitOfWork.Repository<EscalationStatus>().GetAllAsync();
            var resolved = statuses.FirstOrDefault(s =>
                string.Equals(s.Name.Value, "Resuelto", System.StringComparison.OrdinalIgnoreCase));

            escalation.Resolve();
            if (resolved is not null)
                escalation.ChangeStatus(resolved.Id);

            _unitOfWork.ChatEscalations.Update(escalation);

            // Usar la sesión ya trackeada por el Include para evitar conflictos de tracking.
            await SetSessionStatusAsync(escalation.ChatSession, escalation.ChatSessionId, "Cerrada");

            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id) ?? ToDto(escalation);
        }

        private async Task SetSessionStatusAsync(ChatSession? trackedSession, int chatSessionId, string statusName)
        {
            var session = trackedSession ?? await _unitOfWork.ChatSessions.GetByIdAsync(chatSessionId);
            if (session is null) return;

            var sessionStatuses = await _unitOfWork.Repository<ChatSessionStatus>().GetAllAsync();
            var target = sessionStatuses.FirstOrDefault(s =>
                string.Equals(s.Name.Value, statusName, System.StringComparison.OrdinalIgnoreCase));

            if (target is null) return;

            session.ChangeStatus(target.Id);
            _unitOfWork.ChatSessions.Update(session);
        }

        private async Task SetSessionStatusAsync(int chatSessionId, string statusName)
            => await SetSessionStatusAsync(null, chatSessionId, statusName);

        private static ChatEscalationDto ToDto(ChatEscalation escalation) => new()
        {
            ChatEscalationId = escalation.Id,
            ChatSessionId = escalation.ChatSessionId,
            CustomerId = escalation.ChatSession?.CustomerId,
            CustomerName = escalation.ChatSession?.Customer?.Name.Value,
            Reason = escalation.Reason?.Value,
            StatusName = escalation.EscalationStatus?.Name.Value ?? string.Empty,
            AssignedUserName = escalation.AssignedUser?.Name.Value,
            CreatedAt = escalation.CreatedAt,
            ResolvedAt = escalation.ResolvedAt?.Value,
        };
    }
}
