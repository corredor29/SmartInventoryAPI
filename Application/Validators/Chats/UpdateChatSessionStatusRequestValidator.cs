using FluentValidation;
using Application.DTOs.Chats.ChatSessionStatus;

namespace Application.Validators.Chats
{
    public class UpdateChatSessionStatusRequestValidator : AbstractValidator<UpdateChatSessionStatusRequest>
    {
        public UpdateChatSessionStatusRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del estado de sesión es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}