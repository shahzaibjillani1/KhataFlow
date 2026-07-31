using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record ProductUpdateRequest(Guid id,
    string ProductName,
    Guid CategoryId,
    decimal Price,
    int Stock,
    InventoryStatus InventoryStatus
    );
