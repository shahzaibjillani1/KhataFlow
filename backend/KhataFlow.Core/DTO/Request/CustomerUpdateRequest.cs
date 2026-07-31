namespace KhataFlow.Core.DTO;

public record CustomerUpdateRequest(
    Guid Id,
    string Name,
    string PhoneNumber,
    string Address,
    Guid businessId,
    DateTime LastVisit,
    decimal TotalPurchases,
    decimal UdharAmount
    );
