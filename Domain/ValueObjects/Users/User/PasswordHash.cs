using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Users.User
{
    public record PasswordHash
    {
        public string Value { get; }
        public PasswordHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Password hash cannot be empty.");
            if (value.Length > 255)
                throw new ArgumentException("Password hash cannot exceed 255 characters.");
            Value = value;
        }
    }
}