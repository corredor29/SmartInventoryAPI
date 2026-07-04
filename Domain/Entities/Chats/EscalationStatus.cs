using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ValueObject.Chats.EscalationStatus;
namespace Domain.Entities.Chats
{
    public sealed class EscalationStatus : BaseEntity
    {
        public EscalationStatusName Name { get; private set; } = null!;

        public ICollection<ChatEscalation> Escalations { get; private set; } = new List<ChatEscalation>();

        private EscalationStatus() { }

        public EscalationStatus(EscalationStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void Update(EscalationStatusName name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}