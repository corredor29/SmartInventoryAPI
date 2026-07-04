using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.EscalationStatus
{
    public record EscalationStatusName
    {
        public string Value { get; }
        public EscalationStatusName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Escalation status name cannot be empty.");
            if (value.Length > 50)
                throw new ArgumentException("Escalation status name cannot exceed 50 characters.");
            Value = value.Trim();
        }
    }
}