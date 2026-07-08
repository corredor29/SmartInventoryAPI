using FluentValidation;
using Application.DTOs.Chats.SenderType;

namespace Application.Validators.Chats
{
    public class UpdateSenderTypeRequestValidator : AbstractValidator<UpdateSenderTypeRequest>
    {
        public UpdateSenderTypeRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del tipo de remitente es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}