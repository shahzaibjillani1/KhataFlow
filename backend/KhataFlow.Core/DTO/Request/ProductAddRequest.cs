using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record ProductAddRequest(
    string ProductName,
    Guid CategoryId,
    decimal Price,
    int Stock,
    InventoryStatus InventoryStatus
);
