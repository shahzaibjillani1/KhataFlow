namespace KhataFlow.Core.DTO.Response;

public class LedgerEntryResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public decimal RunningBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LedgerItemResponse>? Items { get; set; }
}