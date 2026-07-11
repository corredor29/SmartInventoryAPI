using FluentValidation;
using Application.DTOs.Users.User;

namespace Application.Validators.Users
{
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es obligatorio.")
                .EmailAddress().WithMessage("El email no tiene un formato válido.");

            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("Debe seleccionar un rol válido.");

            RuleFor(x => x.Password)
                .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Password));
        }
    }
}
