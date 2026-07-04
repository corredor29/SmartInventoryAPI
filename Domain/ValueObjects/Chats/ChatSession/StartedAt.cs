using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.ChatSession
{
    public record StartedAt
    {
        public DateTime Value { get; }
        public StartedAt(DateTime value)
        {
            if (value > DateTime.UtcNow.AddMinutes(5))
                throw new ArgumentException("Started at cannot be in the future.");
            Value = value;
        }

        public static StartedAt Now() => new(DateTime.UtcNow);
    }
}