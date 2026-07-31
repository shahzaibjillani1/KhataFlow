using KhataFlow.Core.Domain.Entities;

public interface ILedgerRepository
{
    Task<LedgerEntry> AddAsync(LedgerEntry entry);
    Task<List<LedgerEntry>> GetByCustomerIdAsync(Guid customerId, Guid businessId, CancellationToken ct = default);
    Task<List<LedgerEntry>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<decimal> GetOutstandingBalanceAsync(Guid customerId, Guid businessId, CancellationToken ct = default);
    Task<decimal> GetOutstandingBalanceForBusinessAsync(Guid businessId, CancellationToken ct = default);
    Task<decimal> GetExpensesInRangeAsync(Guid businessId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<LedgerPageResult> GetPagedByCustomerIdAsync(
        Guid customerId, Guid businessId, DateTime? before, int limit, CancellationToken ct = default);

    Task<(decimal TotalPurchases, decimal TotalPaid)> GetTotalsAsync(
        Guid customerId, Guid businessId, CancellationToken ct = default);
}

public record LedgerPageResult(List<LedgerEntry> Entries, bool HasMore, decimal BalanceBeforeBatch);