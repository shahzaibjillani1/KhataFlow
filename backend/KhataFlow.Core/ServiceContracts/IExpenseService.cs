using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface IExpenseService
{
    Task<ExpenseResponse> AddExpenseAsync(Guid businessId, ExpenseAddRequest request);
    Task<List<ExpenseResponse>> GetAllExpensesAsync(Guid businessId);
    Task<PaginatedResponse<ExpenseResponse>> GetExpensesPagedAsync(Guid businessId, int pageNumber, int pageSize);
    Task<decimal> GetTotalExpensesAsync(Guid businessId, DateOnly from, DateOnly to);
    Task<List<CategoryExpenseSummaryResponse>> GetCategoryBreakdownAsync(Guid businessId, DateOnly from, DateOnly to);
    Task<bool> DeleteExpenseAsync(Guid businessId, Guid expenseId);
}