using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace Domain.ValueObject.Invoices.Invoice
{
    public partial record InvoiceNumber
    {
        public string Value { get; }
        public InvoiceNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Invoice number cannot be empty.");
            if (!FormatRegex().IsMatch(value))
                throw new ArgumentException($"Invoice number '{value}' does not match the expected format (e.g. FAC-000001).");
            Value = value;
        }

        public static InvoiceNumber FromSequence(int sequence) => new($"FAC-{sequence:D6}");

        [GeneratedRegex(@"^FAC-\d{6}$")]
        private static partial Regex FormatRegex();
    }
}