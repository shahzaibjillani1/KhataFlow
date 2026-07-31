using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO;

public record ChangeSubscriptionRequest(
    SubscriptionPlanType NewPlan,
    DateTime? CustomExpiryDate 
);
