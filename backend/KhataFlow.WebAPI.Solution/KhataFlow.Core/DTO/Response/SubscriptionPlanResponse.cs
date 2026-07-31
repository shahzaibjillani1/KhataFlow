using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public record SubscriptionPlanResponse(
    Guid Id,
    string PlanName,
    string? PlanNameUr,
    decimal MonthlyPrice,
    List<string> Features,
    List<string> FeaturesUr,
    SubscriptionPlanType PlanType,
    bool IsActive,
    int UserCount,
    decimal TotalRevenue
);