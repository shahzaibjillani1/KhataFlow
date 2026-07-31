using FluentValidation;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Validators;

public class ExpenseAddRequestValidator : AbstractValidator<ExpenseAddRequest>
{
    public ExpenseAddRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(localizer["Validation.Expense.Title.Required"])
            .MaximumLength(150).WithMessage(localizer["Validation.Expense.Title.MaxLength"]);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(localizer["Validation.Expense.Amount.GreaterThanZero"]);

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage(localizer["Validation.Expense.Category.Invalid"]);

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage(localizer["Validation.Notes.MaxLength"]);
    }
}