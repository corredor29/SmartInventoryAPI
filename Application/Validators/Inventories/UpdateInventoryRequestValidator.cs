using FluentValidation;
using Application.DTOs.Inventories.Inventory;

namespace Application.Validators.Inventories
{
    public class UpdateInventoryRequestValidator : AbstractValidator<UpdateInventoryRequest>
    {
        public UpdateInventoryRequestValidator()
        {
            RuleFor(x => x.QuantityChange)
                .NotEqual(0).WithMessage("La cantidad de ajuste no puede ser 0.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("El motivo del ajuste es obligatorio.")
                .MaximumLength(255).WithMessage("El motivo no puede exceder 255 caracteres.");
        }
    }
}