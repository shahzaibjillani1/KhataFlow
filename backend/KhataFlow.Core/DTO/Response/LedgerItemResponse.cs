namespace KhataFlow.Core.DTO.Response;

public class LedgerItemResponse
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductNameUr { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}