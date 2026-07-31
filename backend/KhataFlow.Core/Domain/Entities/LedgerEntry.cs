using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Domain.Entities;

public class LedgerEntry : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public decimal Amount { get; set; }
    public LedgerEntryType EntryType { get; set; }

    public string? Notes { get; set; }
    public string? NotesUr { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
}