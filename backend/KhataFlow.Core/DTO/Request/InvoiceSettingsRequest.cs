using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public class InvoiceSettingsRequest
{
    public string? LogoUrl { get; set; }
    public string PrimaryColorHex { get; set; } = "#7C3AED";
    public string AccentColorHex { get; set; } = "#F3E8FF";
    public string? FooterNote { get; set; }
    public bool ShowBusinessAddress { get; set; } = true;
    public string FontFamily { get; set; } = "Inter";
    public InvoiceTemplateStyle Style { get; set; } = InvoiceTemplateStyle.Classic;
}