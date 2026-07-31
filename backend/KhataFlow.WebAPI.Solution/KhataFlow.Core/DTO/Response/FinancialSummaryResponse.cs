namespace KhataFlow.Core.DTO.Response;

public record FinancialSummaryResponse(
    DateOnly From,
    DateOnly To,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal GrossProfit,
    decimal TotalOutstanding,
    int TotalOrders,
    int TotalCustomers,
    decimal AverageOrderValue
);