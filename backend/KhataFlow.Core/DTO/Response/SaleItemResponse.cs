namespace KhataFlow.Core.DTO.Response;

public record SaleItemResponse(
    Guid ProductId,
    string ProductName,
    string ProductNameUr,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);