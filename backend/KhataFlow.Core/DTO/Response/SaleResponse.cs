using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public record SaleResponse(
    Guid Id,
    string InvoiceNumber,
    DateTime Date,
    string CustomerName,
    string? CustomerNameUr,
    decimal TotalAmount,
    int ItemCount,
    PaymentStatus PaymentStatus,
    List<SaleItemResponse> Items
);