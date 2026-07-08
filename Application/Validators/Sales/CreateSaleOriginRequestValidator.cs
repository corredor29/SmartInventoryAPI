using FluentValidation;
using Application.DTOs.Sales.SaleOrigin;

namespace Application.Validators.Sales
{
    public class CreateSaleOriginRequestValidator : AbstractValidator<CreateSaleOriginRequest>
    {
        public CreateSaleOriginRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del origen de venta es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}