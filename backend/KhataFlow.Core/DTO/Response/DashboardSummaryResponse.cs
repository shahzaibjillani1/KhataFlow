namespace KhataFlow.Core.DTO.Response;

public class DashboardSummaryResponse
{
    public decimal TodaySales { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal Profit { get; set; }
    public int Customers { get; set; }
    public int Products { get; set; }
    public int LowStock { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int TodayOrders { get; set; }
}
