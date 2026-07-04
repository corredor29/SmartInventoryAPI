using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace Domain.ValueObject.Users.User
{
    public partial record Email
    {
        public string Value { get; }
        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty.");
            if (value.Length > 150)
                throw new ArgumentException("Email cannot exceed 150 characters.");
            if (!EmailRegex().IsMatch(value))
                throw new ArgumentException($"Email '{value}' is not a valid email address.");
            Value = value.Trim().ToLowerInvariant();
        }

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailRegex();
    }
}