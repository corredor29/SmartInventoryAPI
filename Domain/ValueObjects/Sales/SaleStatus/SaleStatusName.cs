using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.ValueObjects.Sales.SaleStatus
{
    public sealed record SaleStatusName
    {
        public string Value { get; private set; } = default!;

        private SaleStatusName() { }

        private SaleStatusName(string value)
        {
            Value = value;
        }

        public static SaleStatusName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Sale status name cannot be empty.");

            value = value.Trim();

            if (value.Length > 50)
                throw new ArgumentException("Sale status name cannot exceed 50 characters.");

            var allowed = new[]
            {
                "Pendiente",
                "Completada",
                "Cancelada"
            };

            if (!allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid sale status.");

            return new SaleStatusName(value);
        }

        public override string ToString() => Value;
    }
}