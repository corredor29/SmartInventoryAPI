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
        }
    }
}