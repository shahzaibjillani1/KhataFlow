using FluentValidation;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage(localizer["Validation.CustomerId.Required"]);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(localizer["Validation.Amount.GreaterThanZero"]);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage(localizer["Validation.Notes.MaxLength"])
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}