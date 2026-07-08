using FluentValidation;
using Application.DTOs.Chats.ChatEscalation;

namespace Application.Validators.Chats
{
    public class AssignChatEscalationRequestValidator : AbstractValidator<AssignChatEscalationRequest>
    {
        public AssignChatEscalationRequestValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Debe indicar un asesor válido.");
        }
    }
}