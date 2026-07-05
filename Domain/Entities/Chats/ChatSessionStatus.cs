using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObjects.Chats.ChatSessionStatus;

namespace Domain.Entities.Chats
{
    public class ChatSessionStatus : BaseEntity
    {
        public ChatSessionStatusName Name { get; private set; } = null!;

        private readonly List<ChatSession> _chatSessions = new();
        public IReadOnlyCollection<ChatSession> ChatSessions => _chatSessions.AsReadOnly();

        private ChatSessionStatus() { }

        public ChatSessionStatus(ChatSessionStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void UpdateName(ChatSessionStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}