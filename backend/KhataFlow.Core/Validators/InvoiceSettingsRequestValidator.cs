using FluentValidation;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace KhataFlow.Core.Validators;

public partial class InvoiceSettingsRequestValidator : AbstractValidator<InvoiceSettingsRequest>
{
    [GeneratedRegex("^#([0-9A-Fa-f]{6})$")]
    private static partial Regex HexColorRegex();

    public InvoiceSettingsRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.PrimaryColorHex)
            .NotEmpty().WithMessage(localizer["InvoiceSettings.PrimaryColor.Required"])
            .Matches(HexColorRegex()).WithMessage(localizer["InvoiceSettings.PrimaryColor.InvalidFormat"]);

        RuleFor(x => x.AccentColorHex)
            .NotEmpty().WithMessage(localizer["InvoiceSettings.AccentColor.Required"])
            .Matches(HexColorRegex()).WithMessage(localizer["InvoiceSettings.AccentColor.InvalidFormat"]);

        RuleFor(x => x.FontFamily)
            .NotEmpty().WithMessage(localizer["InvoiceSettings.FontFamily.Required"])
            .MaximumLength(50);

        RuleFor(x => x.FooterNote)
            .MaximumLength(300).WithMessage(localizer["InvoiceSettings.FooterNote.TooLong"]);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(2048)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage(localizer["InvoiceSettings.LogoUrl.Invalid"]);

        RuleFor(x => x.Style).IsInEnum();
    }
}