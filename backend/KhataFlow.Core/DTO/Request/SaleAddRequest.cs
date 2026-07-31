using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record SaleItemRequest(
    Guid ProductId,
    int Quantity
    );

public record SaleAddRequest(
    Guid? CustomerId,
    PaymentStatus PaymentStatus,
    string? Note,
    List<SaleItemRequest> Items
    );