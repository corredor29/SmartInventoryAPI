using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.ChatMessage
{
    public record SentAt
    {
        public DateTime Value { get; }
        public SentAt(DateTime value)
        {
            if (value > DateTime.UtcNow.AddMinutes(5))
                throw new ArgumentException("Sent at cannot be in the future.");
            Value = value;
        }

        public static SentAt Now() => new(DateTime.UtcNow);
    }
}