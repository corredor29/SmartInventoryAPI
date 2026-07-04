using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.ChatMessage
{
    public record MessageContent
    {
        public string Value { get; }
        public MessageContent(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Message content cannot be empty.");
            if (value.Length > 4000)
                throw new ArgumentException("Message content cannot exceed 4000 characters.");
            Value = value.Trim();
        }
    }
}