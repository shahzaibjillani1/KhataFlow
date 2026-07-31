namespace KhataFlow.Core.DTO.Response;

public record DailySalesSummaryResponse(
    DateOnly Date,
    int TotalTransactions,
    decimal TotalRevenue
);