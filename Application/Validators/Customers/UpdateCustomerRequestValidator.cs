using FluentValidation;
using Application.DTOs.Customers.Customer;

namespace Application.Validators.Customers
{
    public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
    {
        public UpdateCustomerRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El email no tiene un formato válido.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(30).WithMessage("El teléfono no puede exceder 30 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.DocumentNumber)
                .MaximumLength(50).WithMessage("El documento no puede exceder 50 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.DocumentNumber));
        }
    }
}