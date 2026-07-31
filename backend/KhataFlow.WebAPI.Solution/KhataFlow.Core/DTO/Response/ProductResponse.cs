using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public record ProductResponse
{
    public Guid Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductNameUr { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? CategoryNameUr { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public InventoryStatus InventoryStatus { get; init; }
}