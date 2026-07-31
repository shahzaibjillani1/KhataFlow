using FluentValidation;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class ProductUpdateRequestValidator : AbstractValidator<ProductUpdateRequest>
{
    public ProductUpdateRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.id)
            .NotEmpty().WithMessage(localizer["Validation.Product.Id.Required"]);

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage(localizer["Validation.Product.Name.Required"])
            .MaximumLength(100).WithMessage(localizer["Validation.Product.Name.MaxLength"]);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(localizer["Validation.Product.CategoryId.Required"]);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage(localizer["Validation.Product.Price.GreaterThanZero"])
            .LessThanOrEqualTo(999999.99m).WithMessage(localizer["Validation.Product.Price.TooHigh"]);

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage(localizer["Validation.Product.Stock.NotNegative"]);
    }
}