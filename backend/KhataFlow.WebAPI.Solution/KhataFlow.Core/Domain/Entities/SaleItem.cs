using KhataFlow.Core.Domain.Common;

namespace KhataFlow.Core.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } 

    public decimal Total => Quantity * UnitPrice;
}