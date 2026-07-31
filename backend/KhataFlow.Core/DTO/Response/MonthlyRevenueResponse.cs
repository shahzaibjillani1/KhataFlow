namespace KhataFlow.Core.DTO.Response;

public record MonthlyRevenueResponse
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
}
