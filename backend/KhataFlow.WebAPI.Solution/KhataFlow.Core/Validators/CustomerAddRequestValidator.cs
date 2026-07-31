using FluentValidation;
using KhataFlow.Core.DTO;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class CustomerAddRequestValidator : AbstractValidator<CustomerAddRequest>
{
    public CustomerAddRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["Validation.CustomerName.Required"])
            .MaximumLength(100).WithMessage(localizer["Validation.CustomerName.MaxLength"]);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage(localizer["Validation.PhoneNumber.Required"])
            .Matches(@"^(\+92|0)3[0-9]{2}[-\s]?[0-9]{7}$")
            .WithMessage(localizer["Validation.PhoneNumber.InvalidPakistani"]);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage(localizer["Validation.Address.Required"])
            .MaximumLength(250).WithMessage(localizer["Validation.Address.MaxLength250"]);

        RuleFor(x => x.LastVisit)
            .NotEmpty().WithMessage(localizer["Validation.LastVisit.Required"])
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage(localizer["Validation.LastVisit.NotFuture"]);

        RuleFor(x => x.TotalPurchases)
            .GreaterThanOrEqualTo(0).WithMessage(localizer["Validation.TotalPurchases.NotNegative"]);

        RuleFor(x => x.UdharAmount)
            .GreaterThanOrEqualTo(0).WithMessage(localizer["Validation.UdharAmount.NotNegative"]);
    }
}