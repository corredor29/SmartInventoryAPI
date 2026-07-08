using FluentValidation;
using Application.DTOs.Chats.ChatEscalation;

namespace Application.Validators.Chats
{
    public class CreateChatEscalationRequestValidator : AbstractValidator<CreateChatEscalationRequest>
    {
        public CreateChatEscalationRequestValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El sessionId es obligatorio.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("El motivo del escalamiento es obligatorio.")
                .MaximumLength(500).WithMessage("El motivo no puede exceder 500 caracteres.");
        }
    }
}