using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObject.Products.MovementType
{
    public sealed record MovementTypeName
    {
        public string Value { get; private set; } = default!;

        private MovementTypeName() { }

        private MovementTypeName(string value)
        {
            Value = value;
        }

        public static MovementTypeName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Movement type name cannot be empty.");

            value = value.Trim();

            if (value.Length > 50)
                throw new ArgumentException("Movement type name cannot exceed 50 characters.");

            string[] validValues = { "Entrada", "Salida", "Ajuste" };

            if (!validValues.Contains(value))
                throw new ArgumentException("Movement type must be Entrada, Salida or Ajuste.");

            return new MovementTypeName(value);
        }

        public override string ToString() => Value;
    
        
    }
}