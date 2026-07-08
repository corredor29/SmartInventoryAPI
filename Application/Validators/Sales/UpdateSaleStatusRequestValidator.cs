using FluentValidation;
using Application.DTOs.Sales.SaleStatus;

namespace Application.Validators.Sales
{
    public class UpdateSaleStatusRequestValidator : AbstractValidator<UpdateSaleStatusRequest>
    {
        public UpdateSaleStatusRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del estado de venta es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}