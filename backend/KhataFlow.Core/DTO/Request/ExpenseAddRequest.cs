using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record ExpenseAddRequest
{
    public string Title { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public ExpenseCategory Category { get; init; }
    public string? Note { get; init; }
    public DateTime? Date { get; init; } 
}