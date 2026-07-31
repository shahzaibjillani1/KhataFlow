namespace KhataFlow.Core.DTO.Response;

public record WeeklySalesResponse
{
    public string Day { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
}
