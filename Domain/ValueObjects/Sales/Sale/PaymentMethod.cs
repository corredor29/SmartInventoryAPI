using System;

namespace Domain.ValueObject.Sales.Sale
{
    public sealed record PaymentMethod
    {
        public string Value { get; }

        private PaymentMethod(string value)
        {
            Value = value;
        }

        public static PaymentMethod Efectivo { get; } = new("Efectivo");
        public static PaymentMethod Tarjeta { get; } = new("Tarjeta");

        public static PaymentMethod Create(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Payment method is required.");

            var normalized = value.Trim().ToLowerInvariant();
            // Acepta variantes: "tarjeta", "tarjeta crédito", "credito", "debito", etc.
            if (normalized is "efectivo" or "cash")
                return Efectivo;
            if (normalized.Contains("tarjeta", StringComparison.Ordinal)
                || normalized is "card" or "credito" or "crédito" or "debito" or "débito"
                || normalized.Contains("credito", StringComparison.Ordinal)
                || normalized.Contains("crédito", StringComparison.Ordinal)
                || normalized.Contains("debito", StringComparison.Ordinal)
                || normalized.Contains("débito", StringComparison.Ordinal))
                return Tarjeta;

            throw new ArgumentException(
                $"Payment method '{value}' is not supported. Use 'Efectivo' or 'Tarjeta'.");
        }

        public override string ToString() => Value;
    }
}
