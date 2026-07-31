using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Exceptions;

namespace KhataFlow.Core.Domain.Entities;

public class Product : BaseEntity
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductNameUr { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int LowStockThreshold { get; set; } = 10;

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public InventoryStatus InventoryStatus => Stock == 0
        ? InventoryStatus.OutOfStock
        : Stock <= LowStockThreshold
            ? InventoryStatus.LowStock
            : InventoryStatus.InStock;

    public void DeductStock(int quantity)
    {
        if (quantity > Stock)
            throw DomainException.ForResource("Domain.Product.InsufficientStock", ProductName);

        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RestockAsync(int quantity)
    {
        if (quantity <= 0)
            throw DomainException.ForResource("Domain.Product.RestockQuantity.GreaterThanZero");

        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}