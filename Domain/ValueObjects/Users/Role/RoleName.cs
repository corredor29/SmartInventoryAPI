using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObject.Users.Role
{
    public record RoleName
    {
        public string Value { get; }
        public RoleName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Role name cannot be empty.");
            if (value.Length > 50)
                throw new ArgumentException("Role name cannot exceed 50 characters.");
            Value = value.Trim();
        }
    }

}