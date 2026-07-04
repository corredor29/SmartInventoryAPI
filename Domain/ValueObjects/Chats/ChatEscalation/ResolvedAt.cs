using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.ChatEscalation
{
    public record ResolvedAt
    {
        public DateTime Value { get; }
        public ResolvedAt(DateTime value)
        {
            if (value > DateTime.UtcNow.AddMinutes(5))
                throw new ArgumentException("Resolved at cannot be in the future.");
            Value = value;
        }

        public static ResolvedAt Now() => new(DateTime.UtcNow);
    }
}