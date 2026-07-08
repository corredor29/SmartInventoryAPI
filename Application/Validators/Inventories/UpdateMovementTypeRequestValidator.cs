using FluentValidation;
using Application.DTOs.Inventories.MovementType;

namespace Application.Validators.Inventories
{
    public class UpdateMovementTypeRequestValidator : AbstractValidator<UpdateMovementTypeRequest>
    {
        public UpdateMovementTypeRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del tipo de movimiento es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}