using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Customers;
using Domain.ValueObject.Chats.ChatSession;
namespace Domain.Entities.Chats
{
    public sealed class ChatSession : BaseEntity
    {
        public int?      CustomerId          { get; private set; }
        public int       ChatSessionStatusId { get; private set; }
        public StartedAt StartedAt           { get; private set; } = null!;

        public Customer?          Customer          { get; private set; }
        public ChatSessionStatus  ChatSessionStatus { get; private set; } = null!;
        public ChatEscalation?    Escalation        { get; private set; }

        private readonly List<ChatMessage> _messages = new();
        public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

        private ChatSession() { }

        public ChatSession(int chatSessionStatusId, int? customerId = null, StartedAt? startedAt = null)
        {
            ChatSessionStatusId = chatSessionStatusId > 0 ? chatSessionStatusId : throw new ArgumentException("ChatSessionStatusId must be greater than 0.");
            CustomerId          = customerId;
            StartedAt           = startedAt ?? StartedAt.Now();
        }

        public void AddMessage(ChatMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            _messages.Add(message);
        }

        public void ChangeStatus(int chatSessionStatusId)
        {
            ChatSessionStatusId = chatSessionStatusId > 0 ? chatSessionStatusId : throw new ArgumentException("ChatSessionStatusId must be greater than 0.");
        }

        public void LinkCustomer(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than 0.");
            CustomerId ??= customerId;
        }
    }
}