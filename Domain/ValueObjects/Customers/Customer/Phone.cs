using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace Domain.ValueObject.Customers.Customer
{
    public partial record Phone
    {
        public string Value { get; }
        public Phone(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Phone cannot be empty.");
            if (value.Length > 30)
                throw new ArgumentException("Phone cannot exceed 30 characters.");
            if (!PhoneRegex().IsMatch(value))
                throw new ArgumentException($"Phone '{value}' is not a valid phone number.");
            Value = value.Trim();
        }

        [GeneratedRegex(@"^[0-9+\-\s()]{6,30}$")]
        private static partial Regex PhoneRegex();
    }
}