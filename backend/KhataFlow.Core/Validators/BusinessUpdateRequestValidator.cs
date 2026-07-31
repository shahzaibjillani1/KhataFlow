using FluentValidation;
using KhataFlow.Core.DTO;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class BusinessUpdateRequestValidator : AbstractValidator<BusinessUpdateRequest>
{
    public BusinessUpdateRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(localizer["Validation.BusinessId.Required"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["Validation.BusinessName.Required"])
            .MinimumLength(2).WithMessage(localizer["Validation.BusinessName.MinLength"])
            .MaximumLength(100).WithMessage(localizer["Validation.BusinessName.MaxLength"]);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(localizer["Validation.Email.Required"])
            .EmailAddress().WithMessage(localizer["Validation.Email.Invalid"])
            .MaximumLength(200).WithMessage(localizer["Validation.Email.MaxLength"]);
    }
}