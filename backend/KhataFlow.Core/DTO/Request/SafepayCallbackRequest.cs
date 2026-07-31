namespace KhataFlow.Core.DTO.Request;

public class SafepayCallbackRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
    public string Tracker { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}