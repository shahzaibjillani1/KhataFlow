using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public record ExpenseResponse(
    Guid Id,
    string Title,
    string? TitleUr,
    decimal Amount,
    ExpenseCategory Category,
    string? Note,
    string? NoteUr,
    DateTime Date);