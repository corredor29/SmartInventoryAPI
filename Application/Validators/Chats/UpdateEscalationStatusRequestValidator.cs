using FluentValidation;
using Application.DTOs.Chats.EscalationStatus;

namespace Application.Validators.Chats
{
    public class UpdateEscalationStatusRequestValidator : AbstractValidator<UpdateEscalationStatusRequest>
    {
        public UpdateEscalationStatusRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del estado de escalamiento es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}