namespace KhataFlow.Core.DTO.Response;

public record CustomerResponse(
    Guid? Id,
    string Name,
    string? NameUr,
    string PhoneNumber,
    string Address,
    string? AddressUr,
    DateTime LastVisit,
    decimal TotalPurchases,
    decimal UdharAmount,
    string PublicToken);