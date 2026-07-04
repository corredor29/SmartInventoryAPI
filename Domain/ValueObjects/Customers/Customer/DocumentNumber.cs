using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Customers.Customer
{
    public record DocumentNumber
    {
        public string Value { get; }
        public DocumentNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Document number cannot be empty.");
            if (value.Length > 50)
                throw new ArgumentException("Document number cannot exceed 50 characters.");
            Value = value.Trim();
        }
    }
}