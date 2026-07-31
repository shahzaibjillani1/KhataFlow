using FluentValidation;
using KhataFlow.Core.DTO;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class CustomerUpdateRequestValidator : AbstractValidator<CustomerUpdateRequest>
{
    public CustomerUpdateRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(localizer["Validation.CustomerId.RequiredUpdate"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["Validation.CustomerName.Required"])
            .MaximumLength(100).WithMessage(localizer["Validation.CustomerName.MaxLength"]);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage(localizer["Validation.PhoneNumber.Required"])
            .Matches(@"^\+?[0-9]{7,15}$").WithMessage(localizer["Validation.PhoneNumber.InvalidGeneric"]);

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