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

        public async Task<ChatEscalationDto> CreateAsync(CreateChatEscalationRequest request)
        {
            // Busca el estado inicial "Pendiente" por nombre
            var statuses = await _unitOfWork.Repository<EscalationStatus>().GetAllAsync();
            var pendingStatus = statuses.FirstOrDefault(s =>
                string.Equals(s.Name.Value, "Pendiente", System.StringComparison.OrdinalIgnoreCase));

            if (pendingStatus is null)
                throw new System.InvalidOperationException("No se encontró el estado 'Pendiente' en EscalationStatus.");

            // Busca la sesión de chat asociada al sessionId (aquí sessionId es el Id de ChatSession)
            var chatSessionId = int.Parse(request.SessionId);

            var escalation = new ChatEscalation(
                chatSessionId,
                pendingStatus.Id,
                new EscalationReason(request.Reason)
            );

            await _unitOfWork.ChatEscalations.AddAsync(escalation);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(escalation);
        }

        public async Task<ChatEscalationDto?> AssignAsync(int id, AssignChatEscalationRequest request)
        {
            var escalation = await _unitOfWork.ChatEscalations.GetByIdAsync(id);
            if (escalation is null) return null;

            escalation.AssignTo(request.UserId);
            _unitOfWork.ChatEscalations.Update(escalation);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(escalation);
        }

        public async Task<ChatEscalationDto?> ResolveAsync(int id)
        {
            var escalation = await _unitOfWork.ChatEscalations.GetByIdAsync(id);
            if (escalation is null) return null;

            escalation.Resolve();
            _unitOfWork.ChatEscalations.Update(escalation);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(escalation);
        }

        private static ChatEscalationDto ToDto(ChatEscalation escalation) => new()
        {
            ChatEscalationId = escalation.Id,
            ChatSessionId = escalation.ChatSessionId,
            Reason = escalation.Reason?.Value,
            StatusName = escalation.EscalationStatus?.Name.Value ?? string.Empty,
            AssignedUserName = escalation.AssignedUser?.Name.Value,
            CreatedAt = escalation.CreatedAt,
            ResolvedAt = escalation.ResolvedAt?.Value,
        };
    }
}