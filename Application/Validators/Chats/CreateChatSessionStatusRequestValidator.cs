using FluentValidation;
using Application.DTOs.Chats.ChatSessionStatus;

namespace Application.Validators.Chats
{
    public class CreateChatSessionStatusRequestValidator : AbstractValidator<CreateChatSessionStatusRequest>
    {
        public CreateChatSessionStatusRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del estado de sesión es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}