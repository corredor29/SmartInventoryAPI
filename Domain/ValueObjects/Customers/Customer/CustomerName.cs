using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Customers.Customer
{
    public record CustomerName
    {
        public string Value { get; }
        public CustomerName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Customer name cannot be empty.");
            if (value.Length > 150)
                throw new ArgumentException("Customer name cannot exceed 150 characters.");
            Value = value.Trim();
        }
    }
}