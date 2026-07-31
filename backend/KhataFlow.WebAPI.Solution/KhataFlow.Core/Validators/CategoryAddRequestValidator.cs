using FluentValidation;
using KhataFlow.Core.DTO;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class CategoryAddRequestValidator : AbstractValidator<CategoryAddRequest>
{
    public CategoryAddRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.CategoryName)
            .NotEmpty().WithMessage(localizer["Validation.CategoryName.Required"])
            .MaximumLength(100).WithMessage(localizer["Validation.CategoryName.MaxLength"]);
    }
}