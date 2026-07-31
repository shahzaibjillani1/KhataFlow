namespace KhataFlow.Core.DTO.Response;

public record FinancialSummaryBuilder(
    DateOnly From,
    DateOnly To,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal TotalOutstanding,
    int TotalOrders,
    int TotalCustomers
);