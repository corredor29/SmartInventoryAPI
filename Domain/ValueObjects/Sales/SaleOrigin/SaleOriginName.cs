using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObject.Sales.SaleOrigin
{
    public sealed record SaleOriginName
    {
        public string Value { get; private set; } = default!;

        private SaleOriginName() { }

        private SaleOriginName(string value)
        {
            Value = value;
        }

        public static SaleOriginName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Sale origin name cannot be empty.");

            value = value.Trim();

            if (value.Length > 50)
                throw new ArgumentException("Sale origin name cannot exceed 50 characters.");

            var allowed = new[]
            {
                "Manual",
                "Chatbot"
            };

            if (!allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid sale origin.");

            return new SaleOriginName(value);
        }

        public override string ToString() => Value;
        

    }
    
}