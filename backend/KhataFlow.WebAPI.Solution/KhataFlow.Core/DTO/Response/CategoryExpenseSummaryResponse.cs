using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public record CategoryExpenseSummaryResponse(ExpenseCategory Category, decimal Total);