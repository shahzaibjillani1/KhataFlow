namespace KhataFlow.Core.DTO;

public record CustomerAddRequest(
    string Name,
    string PhoneNumber,
    string Address,
    Guid BusinessId,
    DateTime LastVisit,
    decimal TotalPurchases,
    decimal UdharAmount
    );
