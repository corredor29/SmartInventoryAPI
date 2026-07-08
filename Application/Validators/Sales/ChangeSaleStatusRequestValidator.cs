using FluentValidation;
using Application.DTOs.Sales.Sale;

namespace Application.Validators.Sales
{
    public class ChangeSaleStatusRequestValidator : AbstractValidator<ChangeSaleStatusRequest>
    {
        public ChangeSaleStatusRequestValidator()
        {
            RuleFor(x => x.SaleStatusId)
                .GreaterThan(0).WithMessage("Debe indicar un estado de venta válido.");
        }
    }
}