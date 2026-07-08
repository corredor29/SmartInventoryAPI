using FluentValidation;
using Application.DTOs.Chats.ChatMessage;

namespace Application.Validators.Chats
{
    public class CreateChatMessageRequestValidator : AbstractValidator<CreateChatMessageRequest>
    {
        public CreateChatMessageRequestValidator()
        {
            RuleFor(x => x.ChatSessionId)
                .GreaterThan(0).WithMessage("ChatSessionId inválido.");

            RuleFor(x => x.SenderTypeId)
                .GreaterThan(0).WithMessage("SenderTypeId inválido.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("El contenido del mensaje no puede estar vacío.")
                .MaximumLength(4000).WithMessage("El mensaje no puede exceder 4000 caracteres.");
        }
    }
}