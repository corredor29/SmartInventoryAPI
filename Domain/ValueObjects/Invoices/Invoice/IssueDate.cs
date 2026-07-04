using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Domain.ValueObject.Invoices.Invoice
{
    public record IssueDate
    {
        public DateTime Value { get; }
        public IssueDate(DateTime value)
        {
            if (value > DateTime.UtcNow.AddMinutes(5))
                throw new ArgumentException("Issue date cannot be in the future.");
            Value = value;
        }

        public static IssueDate Now() => new(DateTime.UtcNow);
    }
}