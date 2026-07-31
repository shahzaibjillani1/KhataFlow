using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Enums;
using KhataFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Expense> AddAsync(Expense expense)
    {
        await _context.Expenses.AddAsync(expense);
        await _context.SaveChangesAsync();
        return expense;
    }

    public async Task<List<Expense>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken ct = default
    )
    {
        return await _context
            .Expenses.AsNoTracking()
            .Where(e => e.BusinessId == businessId && !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<List<Expense>> GetByBusinessIdInRangeAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        return await _context
            .Expenses.AsNoTracking()
            .Where(e =>
                e.BusinessId == businessId
                && !e.IsDeleted
                && e.Date >= from.ToDateTime(TimeOnly.MinValue)
                && e.Date <= to.ToDateTime(TimeOnly.MaxValue)
            )
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetTotalInRangeAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        return await _context
            .Expenses.AsNoTracking()
            .Where(e =>
                e.BusinessId == businessId
                && !e.IsDeleted
                && e.Date >= from.ToDateTime(TimeOnly.MinValue)
                && e.Date <= to.ToDateTime(TimeOnly.MaxValue)
            )
            .SumAsync(e => e.Amount, ct);
    }

    public async Task<Dictionary<ExpenseCategory, decimal>> GetTotalsByCategoryInRangeAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        var grouped = await _context
            .Expenses.AsNoTracking()
            .Where(e =>
                e.BusinessId == businessId
                && !e.IsDeleted
                && e.Date >= from.ToDateTime(TimeOnly.MinValue)
                && e.Date <= to.ToDateTime(TimeOnly.MaxValue)
            )
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(ct);

        return grouped.ToDictionary(g => g.Category, g => g.Total);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var expense = await _context.Expenses.FindAsync([id], ct);
        if (expense is null || expense.IsDeleted)
            return false;

        expense.IsDeleted = true;
        expense.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Expense?> GetByIdAsync(
        Guid id,
        Guid businessId,
        CancellationToken ct = default
    )
    {
        return await _context
            .Expenses.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == businessId && !e.IsDeleted, ct);
    }

    public async Task<(List<Expense>, int)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize)
    {
        var query = _context.Expenses.Where(e => e.BusinessId == businessId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
