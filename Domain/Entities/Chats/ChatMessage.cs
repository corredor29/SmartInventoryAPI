using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObject.Chats.ChatMessage;
namespace Domain.Entities.Chats
{
    public sealed class ChatMessage : BaseEntity
    {
        public int             ChatSessionId { get; private set; }
        public int             SenderTypeId  { get; private set; }
        public MessageContent  Content       { get; private set; } = null!;
        public SentAt          SentAt        { get; private set; } = null!;

        public ChatSession ChatSession { get; private set; } = null!;
        public SenderType  SenderType  { get; private set; } = null!;

        private ChatMessage() { }

        public ChatMessage(int chatSessionId, int senderTypeId, MessageContent content, SentAt? sentAt = null)
        {
            ChatSessionId = chatSessionId > 0 ? chatSessionId : throw new ArgumentException("ChatSessionId must be greater than 0.");
            SenderTypeId  = senderTypeId  > 0 ? senderTypeId  : throw new ArgumentException("SenderTypeId must be greater than 0.");
            Content       = content ?? throw new ArgumentNullException(nameof(content));
            SentAt        = sentAt ?? SentAt.Now();
        }
    }
}