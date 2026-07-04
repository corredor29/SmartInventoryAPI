using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.ChatEscalation
{
    public record EscalationReason
    {
        public string Value { get; }
        public EscalationReason(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > 500)
                throw new ArgumentException("Escalation reason cannot exceed 500 characters.");
            Value = value.Trim();
        }
    }
}