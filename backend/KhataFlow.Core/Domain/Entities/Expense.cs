using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Domain.Entities;

public class Expense : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleUr { get; set; }  
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
    public string? NoteUr { get; set; }
    public ExpenseCategory Category { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
}