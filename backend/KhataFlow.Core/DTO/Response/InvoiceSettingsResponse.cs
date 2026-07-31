using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public class InvoiceSettingsResponse
{
    public Guid Id { get; set; }
    public string? LogoUrl { get; set; }
    public string PrimaryColorHex { get; set; } = string.Empty;
    public string AccentColorHex { get; set; } = string.Empty;
    public string? FooterNote { get; set; }
    public bool ShowBusinessAddress { get; set; }
    public string FontFamily { get; set; } = string.Empty;
    public InvoiceTemplateStyle Style { get; set; }
}