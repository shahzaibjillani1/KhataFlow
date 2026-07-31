using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public record BusinessResponse(
    Guid Id,
    string Name,
    string? NameUr,
    string Email,
    string PhoneNumber,
    string Address,
    string? AddressUr,
    BusinessStatus Status,
    SubscriptionPlanType Plan,
    DateTime SubscriptionExpiry,
    DateTime RegisteredAt
);