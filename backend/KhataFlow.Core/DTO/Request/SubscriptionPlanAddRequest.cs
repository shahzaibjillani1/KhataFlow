using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record SubscriptionPlanAddRequest(
    string PlanName,
    decimal MonthlyPrice,
    List<string> Features,
    SubscriptionPlanType PlanType
);