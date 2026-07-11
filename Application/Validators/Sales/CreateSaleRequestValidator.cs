using FluentValidation;
using Application.DTOs.Sales.Sale;

namespace Application.Validators.Sales
{
    public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
    {
        public CreateSaleRequestValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("La venta debe tener al menos un producto.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0).WithMessage("ProductId inválido.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
            });

            RuleFor(x => x.Origin)
                .NotEmpty().WithMessage("El origen de la venta es obligatorio.");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty()
                .WithMessage("El método de pago es obligatorio (Efectivo o Tarjeta).")
                .When(x => !string.Equals(x.Origin, "Chatbot", System.StringComparison.OrdinalIgnoreCase));

            RuleFor(x => x.PaymentMethod)
                .Must(BeValidPaymentMethod)
                .WithMessage("Método de pago inválido. Use 'Efectivo' o 'Tarjeta'.")
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod));

            When(x => !string.Equals(x.Origin, "Chatbot", System.StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.DeliveryAddress)
                    .NotEmpty().WithMessage("La dirección de entrega es obligatoria.")
                    .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres.");

                RuleFor(x => x.ContactPhone)
                    .NotEmpty().WithMessage("El teléfono de contacto es obligatorio.")
                    .MaximumLength(30).WithMessage("El teléfono no puede exceder 30 caracteres.");

                RuleFor(x => x.ContactDocument)
                    .NotEmpty().WithMessage("El documento de identidad es obligatorio.")
                    .MaximumLength(50).WithMessage("El documento no puede exceder 50 caracteres.");
            });
        }

        private static bool BeValidPaymentMethod(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var normalized = value.Trim().ToLowerInvariant();
            return normalized is "efectivo" or "cash" or "tarjeta" or "card"
                or "credito" or "crédito" or "debito" or "débito";
        }
    }
}
