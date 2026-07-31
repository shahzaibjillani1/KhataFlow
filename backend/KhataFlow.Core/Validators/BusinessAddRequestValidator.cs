using FluentValidation;
using KhataFlow.Core.DTO;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class BusinessAddRequestValidator : AbstractValidator<BusinessAddRequest>
{
    public BusinessAddRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["Validation.BusinessName.Required"])
            .MinimumLength(2).WithMessage(localizer["Validation.BusinessName.MinLength"])
            .MaximumLength(100).WithMessage(localizer["Validation.BusinessName.MaxLength"]);

        RuleFor(x => x.OwnerName)
            .NotEmpty().WithMessage(localizer["Validation.OwnerName.Required"])
            .MinimumLength(2).WithMessage(localizer["Validation.OwnerName.MinLength"])
            .MaximumLength(100).WithMessage(localizer["Validation.OwnerName.MaxLength"]);

        RuleFor(x => x.OwnerEmail)
            .NotEmpty().WithMessage(localizer["Validation.Email.Required"])
            .EmailAddress().WithMessage(localizer["Validation.Email.Invalid"])
            .MaximumLength(200).WithMessage(localizer["Validation.Email.MaxLength"]);

        RuleFor(x => x.phoneNumber)
            .NotEmpty().WithMessage(localizer["Validation.PhoneNumber.Required"])
            .MaximumLength(11).WithMessage(localizer["Validation.PhoneNumber.MaxLength"])
            .Matches(@"^\+?[1-9][0-9]{7,14}$").WithMessage(localizer["Validation.PhoneNumber.Invalid"]); 

        RuleFor(x => x.address)
            .NotEmpty().WithMessage(localizer["Validation.Address.Required"])
            .MaximumLength(200).WithMessage(localizer["Validation.Address.MaxLength"]);

        RuleFor(x => x.Plan)
            .IsInEnum().WithMessage(localizer["Validation.Plan.Invalid"]);
    }
}