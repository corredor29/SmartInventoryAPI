using FluentValidation;
using Application.DTOs.Chats.ChatSession;

namespace Application.Validators.Chats
{
    public class CreateChatSessionRequestValidator : AbstractValidator<CreateChatSessionRequest>
    {
        public CreateChatSessionRequestValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("CustomerId inválido.")
                .When(x => x.CustomerId.HasValue);
        }
    }
}