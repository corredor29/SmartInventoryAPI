using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObjects.Chats.ChatSessionStatus
{
    public sealed record ChatSessionStatusName
    {
        public string Value { get; private set; } = default!;

        private ChatSessionStatusName() { }

        private ChatSessionStatusName(string value)
        {
            Value = value;
        }

        public static ChatSessionStatusName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Chat session status name cannot be empty.");

            value = value.Trim();

            if (value.Length > 50)
                throw new ArgumentException("Chat session status name cannot exceed 50 characters.");

            var allowed = new[]
            {
                "Activa",
                "Escalada",
                "Cerrada"
            };

            if (!allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid chat session status.");

            return new ChatSessionStatusName(value);
        }

        public override string ToString() => Value;
    }
}