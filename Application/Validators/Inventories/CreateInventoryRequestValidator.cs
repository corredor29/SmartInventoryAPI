using FluentValidation;
using Application.DTOs.Inventories.Inventory;

namespace Application.Validators.Inventories
{
    public class CreateInventoryRequestValidator : AbstractValidator<CreateInventoryRequest>
    {
        public CreateInventoryRequestValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Debe indicar un producto válido.");

            RuleFor(x => x.InitialStock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock inicial no puede ser negativo.");
        }
    }
}