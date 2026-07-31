using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO;

public record BusinessAddRequest(
    string Name,
    string OwnerEmail,
    string OwnerName,
    string phoneNumber,
    string address,
    SubscriptionPlanType Plan
);
