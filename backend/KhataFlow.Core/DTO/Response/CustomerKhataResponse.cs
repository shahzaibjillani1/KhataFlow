namespace KhataFlow.Core.DTO.Response;

public class CustomerKhataResponse
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerNameUr { get; set; }
    public string? PhoneNumber { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public List<LedgerEntryResponse> Entries { get; set; } = [];
    public bool HasMore { get; set; }
    public DateTime? NextCursor { get; set; }
}