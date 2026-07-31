using FluentValidation;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class DateRangeValidator : AbstractValidator<DateRange>
{
    public DateRangeValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.From)
            .NotEmpty().WithMessage(localizer["Validation.DateFrom.Required"]);

        RuleFor(x => x.To)
            .NotEmpty().WithMessage(localizer["Validation.DateTo.Required"])
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage(localizer["Validation.DateTo.MustBeAfterFrom"])
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage(localizer["Validation.DateTo.NotFuture"]);
    }
}