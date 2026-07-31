namespace KhataFlow.Core.DTO.Response;

public record PlanRevenueBreakdownResponse(
    Guid PlanId,
    string PlanName,
    decimal Revenue,
    double PercentageOfTotal
);
