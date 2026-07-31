using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Domain.Entities;

public class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
    public string? NoteUr { get; set; }

    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public decimal DiscountAmount { get; set; } = 0;

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

    public decimal Subtotal => Items?.Sum(i => i.Total) ?? 0;
    public decimal GrandTotal => Subtotal - DiscountAmount;

    public decimal TotalAmount => GrandTotal;
}