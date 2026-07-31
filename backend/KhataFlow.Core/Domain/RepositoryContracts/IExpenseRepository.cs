using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface IExpenseRepository
{
    Task<Expense> AddAsync(Expense expense);
    Task<List<Expense>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<List<Expense>> GetByBusinessIdInRangeAsync(Guid businessId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<decimal> GetTotalInRangeAsync(Guid businessId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Expense?> GetByIdAsync(Guid id, Guid businessId, CancellationToken ct = default);
    Task<(List<Expense> Items, int TotalCount)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize);
    Task<Dictionary<ExpenseCategory, decimal>> GetTotalsByCategoryInRangeAsync(Guid businessId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}