using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public record SubscriptionResponse(
    Guid BusinessId,
    string BusinessName,
    string? BusinessNameUr,
    SubscriptionPlanType Plan,
    DateTime StartDate,
    DateTime ExpiryDate,
    bool IsActive
);