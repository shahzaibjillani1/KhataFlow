namespace KhataFlow.Core.DTO.Response;

public class LedgerHistoryItemResponse
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public decimal RunningBalance { get; set; }
    public List<LedgerItemResponse>? Items { get; set; }
}