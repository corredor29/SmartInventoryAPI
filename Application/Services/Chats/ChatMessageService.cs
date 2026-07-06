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

            return session.Messages.Select(ToDto).ToList();
        }

        public async Task<ChatMessageDto> CreateAsync(CreateChatMessageRequest request)
        {
            var message = new ChatMessage(
                request.ChatSessionId,
                request.SenderTypeId,
                new MessageContent(request.Content)
            );

            await _unitOfWork.Repository<ChatMessage>().AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(message);
        }

        private static ChatMessageDto ToDto(ChatMessage message) => new()
        {
            ChatMessageId = message.Id,
            ChatSessionId = message.ChatSessionId,
            SenderTypeName = message.SenderType?.Name.Value ?? string.Empty,
            Content = message.Content.Value,
            SentAt = message.SentAt.Value,
        };
    }
}