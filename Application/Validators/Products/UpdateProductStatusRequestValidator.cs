using FluentValidation;
using Application.DTOs.Products.ProductStatus;

namespace Application.Validators.Products
{
    public class UpdateProductStatusRequestValidator : AbstractValidator<UpdateProductStatusRequest>
    {
        public UpdateProductStatusRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del estado es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.");
        }
    }
}