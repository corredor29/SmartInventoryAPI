using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Chats.SenderType
{
    public record SenderTypeName
    {
        public string Value { get; }
        public SenderTypeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Sender type name cannot be empty.");
            if (value.Length > 50)
                throw new ArgumentException("Sender type name cannot exceed 50 characters.");
            Value = value.Trim();
        }
    }
}