using FluentValidation;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class SaleAddRequestValidator : AbstractValidator<SaleAddRequest>
{
    public SaleAddRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.PaymentStatus)
            .IsInEnum().WithMessage(localizer["Validation.Sale.PaymentStatus.Invalid"]);

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage(localizer["Validation.Notes.MaxLength"]);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage(localizer["Validation.Sale.Items.Required"]);

        RuleForEach(x => x.Items).SetValidator(new SaleItemRequestValidator(localizer));
    }
}

public class SaleItemRequestValidator : AbstractValidator<SaleItemRequest>
{
    public SaleItemRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage(localizer["Validation.SaleItem.ProductId.Required"]);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage(localizer["Validation.SaleItem.Quantity.GreaterThanZero"]);
    }
}