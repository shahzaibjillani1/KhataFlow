namespace KhataFlow.Core.DTO.Request;

public record SubscriptionPlanUpdateRequest(
    Guid Id,
    string PlanName,
    decimal MonthlyPrice,
    List<string> Features,
    bool IsActive
);
