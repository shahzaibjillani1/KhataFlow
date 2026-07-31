using FluentValidation;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class SaleUpdateRequestValidator : AbstractValidator<SaleUpdateRequest>
{
    public SaleUpdateRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage(localizer["Sale.Items.Required"]);

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage(localizer["Sale.ItemQuantity.Invalid"]);
            });

        RuleFor(x => x.PaymentStatus).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage(localizer["Sale.DiscountAmount.Invalid"]);
    }
}
