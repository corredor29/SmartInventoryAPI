using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities.Users;
using Domain.ValueObject.Chats.ChatEscalation;
using ResolvedAtVO = Domain.ValueObject.Chats.ChatEscalation.ResolvedAt;
namespace Domain.Entities.Chats
{
    public sealed class ChatEscalation : BaseEntity
    {
        public int               ChatSessionId      { get; private set; }
        public int               EscalationStatusId { get; private set; }
        public int?              AssignedUserId      { get; private set; }
        public EscalationReason? Reason              { get; private set; }
        public ResolvedAtVO?     ResolvedAt          { get; private set; }

        public ChatSession       ChatSession       { get; private set; } = null!;
        public EscalationStatus  EscalationStatus  { get; private set; } = null!;
        public User?             AssignedUser      { get; private set; }

        private ChatEscalation() { }

        public ChatEscalation(int chatSessionId, int escalationStatusId, EscalationReason? reason = null)
        {
            ChatSessionId      = chatSessionId      > 0 ? chatSessionId      : throw new ArgumentException("ChatSessionId must be greater than 0.");
            EscalationStatusId = escalationStatusId > 0 ? escalationStatusId : throw new ArgumentException("EscalationStatusId must be greater than 0.");
            Reason              = reason;
        }

        public void AssignTo(int userId)
        {
            AssignedUserId = userId > 0 ? userId : throw new ArgumentException("UserId must be greater than 0.");
        }

        public void ChangeStatus(int escalationStatusId)
        {
            EscalationStatusId = escalationStatusId > 0 ? escalationStatusId : throw new ArgumentException("EscalationStatusId must be greater than 0.");
        }

        public void Resolve()
        {
            ResolvedAt = ResolvedAtVO.Now();
        }
    }
}