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
                .MaximumLength(255).WithMessage("El motivo no puede exceder 255 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Reason));
        }
    }
}