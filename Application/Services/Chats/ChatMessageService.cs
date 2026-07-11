using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Contracts.Repositories;
using Application.Contracts.Services.Chats;
using Application.DTOs.Chats.ChatMessage;
using Domain.Entities.Chats;
using Domain.ValueObject.Chats.ChatMessage;

namespace Application.Services.Chats
{
    public class ChatMessageService : IChatMessageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatMessageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ChatMessageDto>> GetBySessionIdAsync(int chatSessionId)
        {
            var session = await _unitOfWork.ChatSessions.GetByIdWithMessagesAsync(chatSessionId);

            if (session is null)
                return new List<ChatMessageDto>();

            return session.Messages
                .OrderBy(m => m.SentAt.Value)
                .ThenBy(m => m.Id)
                .Select(ToDto)
                .ToList();
        }

        public async Task<ChatMessageDto> CreateAsync(CreateChatMessageRequest request)
        {
            var senderType = await _unitOfWork.Repository<SenderType>().GetByIdAsync(request.SenderTypeId);
            if (senderType is null)
                throw new System.InvalidOperationException($"SenderType {request.SenderTypeId} no existe.");

            var message = new ChatMessage(
                request.ChatSessionId,
                request.SenderTypeId,
                new MessageContent(request.Content)
            );

            await _unitOfWork.Repository<ChatMessage>().AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            return new ChatMessageDto
            {
                ChatMessageId = message.Id,
                ChatSessionId = message.ChatSessionId,
                SenderTypeId = message.SenderTypeId,
                SenderTypeName = senderType.Name.Value,
                Content = message.Content.Value,
                SentAt = message.SentAt.Value,
            };
        }

        private static ChatMessageDto ToDto(ChatMessage message)
        {
            var name = message.SenderType?.Name.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = message.SenderTypeId switch
                {
                    1 => "Bot",
                    2 => "Cliente",
                    3 => "Asesor",
                    _ => "Desconocido",
                };
            }

            return new ChatMessageDto
            {
                ChatMessageId = message.Id,
                ChatSessionId = message.ChatSessionId,
                SenderTypeId = message.SenderTypeId,
                SenderTypeName = name,
                Content = message.Content.Value,
                SentAt = message.SentAt.Value,
            };
        }
    }
}