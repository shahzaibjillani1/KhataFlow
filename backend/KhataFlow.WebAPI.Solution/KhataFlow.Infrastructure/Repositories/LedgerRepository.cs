using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Enums;
using KhataFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Repositories;

public class LedgerRepository : ILedgerRepository
{
    private readonly AppDbContext _context;

    public LedgerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerEntry> AddAsync(LedgerEntry entry)
    {
        await _context.LedgerEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<List<LedgerEntry>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        return await _context.LedgerEntries
            .AsNoTracking()
            .Include(e => e.Customer)
            .Where(e => e.BusinessId == businessId && !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<List<LedgerEntry>> GetByCustomerIdAsync(Guid customerId, Guid businessId, CancellationToken ct = default)
    {
        return await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.BusinessId == businessId && !e.IsDeleted)
            .Include(e => e.Sale)
                .ThenInclude(s => s!.Items)
                    .ThenInclude(i => i.Product)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetOutstandingBalanceAsync(Guid customerId, Guid businessId, CancellationToken ct = default)
    {
        return await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.BusinessId == businessId && !e.IsDeleted)
            .SumAsync(e => e.EntryType == LedgerEntryType.Udhar ? e.Amount : -e.Amount, ct);
    }

    public async Task<decimal> GetOutstandingBalanceForBusinessAsync(Guid businessId, CancellationToken ct = default)
    {
        return await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId && !e.IsDeleted)
            .SumAsync(e => e.EntryType == LedgerEntryType.Udhar ? e.Amount : -e.Amount, ct);
    }

    public async Task<decimal> GetExpensesInRangeAsync(Guid businessId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId &&
                        !e.IsDeleted &&
                        (e.EntryType == LedgerEntryType.Cash || e.EntryType == LedgerEntryType.Card) &&
                        e.Date >= from.ToDateTime(TimeOnly.MinValue) &&
                        e.Date <= to.ToDateTime(TimeOnly.MaxValue))
            .SumAsync(e => e.Amount, ct);
    }

    public async Task<LedgerPageResult> GetPagedByCustomerIdAsync(
    Guid customerId, Guid businessId, DateTime? before, int limit, CancellationToken ct = default)
    {
        var query = _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.BusinessId == businessId && !e.IsDeleted);

        if (before.HasValue)
            query = query.Where(e => e.Date < before.Value);

        var batch = await query
            .Include(e => e.Sale)
                .ThenInclude(s => s!.Items)
                    .ThenInclude(i => i.Product)
            .OrderByDescending(e => e.Date)
            .Take(limit + 1) // fetch one extra to detect HasMore without a separate COUNT query
            .ToListAsync(ct);

        var hasMore = batch.Count > limit;
        var page = batch.Take(limit).ToList();

        decimal balanceBeforeBatch = 0;
        if (page.Count > 0)
        {
            var oldestDateInBatch = page[^1].Date; // last item = oldest, since query is OrderByDescending

            balanceBeforeBatch = await _context.LedgerEntries
                .AsNoTracking()
                .Where(e => e.CustomerId == customerId && e.BusinessId == businessId
                            && !e.IsDeleted && e.Date < oldestDateInBatch)
                .SumAsync(e => e.EntryType == LedgerEntryType.Udhar ? e.Amount : -e.Amount, ct);
        }

        page.Reverse(); // oldest -> newest, so running balance reads top-to-bottom like a passbook

        return new LedgerPageResult(page, hasMore, balanceBeforeBatch);
    }

    public async Task<(decimal TotalPurchases, decimal TotalPaid)> GetTotalsAsync(
        Guid customerId, Guid businessId, CancellationToken ct = default)
    {
        var totals = await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.BusinessId == businessId && !e.IsDeleted)
            .GroupBy(e => 1)
            .Select(g => new
            {
                Purchases = g.Where(e => e.EntryType == LedgerEntryType.Udhar).Sum(e => e.Amount),
                Paid = g.Where(e => e.EntryType == LedgerEntryType.Cash || e.EntryType == LedgerEntryType.Card).Sum(e => e.Amount)
            })
            .FirstOrDefaultAsync(ct);

        return (totals?.Purchases ?? 0m, totals?.Paid ?? 0m);
    }
}