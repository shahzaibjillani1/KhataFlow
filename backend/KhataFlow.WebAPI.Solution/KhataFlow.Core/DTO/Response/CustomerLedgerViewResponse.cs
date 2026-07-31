namespace KhataFlow.Core.DTO.Response;

public class CustomerLedgerViewResponse
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerNameUr { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessNameUr { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; } 
    public string Currency { get; set; } = "PKR";
    public List<LedgerHistoryItemResponse> History { get; set; } = new();
}

