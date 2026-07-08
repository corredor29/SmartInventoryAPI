using FluentValidation;
using Application.DTOs.Chats.SenderType;

namespace Application.Validators.Chats
{
    public class CreateSenderTypeRequestValidator : AbstractValidator<CreateSenderTypeRequest>
    {
        public CreateSenderTypeRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del tipo de remitente es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}