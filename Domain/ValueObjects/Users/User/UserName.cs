using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObjects.Users.User
{
    public record UserName
    {
        public string Value { get; }
        public UserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("User name cannot be empty.");
            if (value.Length > 150)
                throw new ArgumentException("User name cannot exceed 150 characters.");
            Value = value.Trim();
        }
    }
}