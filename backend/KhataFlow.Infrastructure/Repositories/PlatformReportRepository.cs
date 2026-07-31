using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Enums;
using KhataFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Repositories;

public class PlatformReportRepository : IPlatformReportRepository
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PlatformReportRepository(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<DateTime>> GetBusinessRegistrationDatesAsync(DateTime since)
    {
        return await _context.Businesses
            .AsNoTracking()
            .Where(b => b.CreatedAt >= since)
            .Select(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DateTime>> GetUserRegistrationDatesAsync(DateTime since)
    {
        return await _userManager.Users
            .Where(u => u.CreatedAt >= since)
            .Select(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SaleRevenuePoint>> GetSaleRevenuePointsAsync(DateTime since)
    {
        var itemTotals = _context.SaleItems
            .GroupBy(si => si.SaleId)
            .Select(g => new { SaleId = g.Key, ItemsTotal = g.Sum(si => si.UnitPrice * si.Quantity) });

        var points = await (
            from s in _context.Sales
            join it in itemTotals on s.Id equals it.SaleId
            where s.Date >= since
            select new SaleRevenuePoint(s.Date, it.ItemsTotal - s.DiscountAmount)
        ).AsNoTracking().ToListAsync();

        return points;
    }

    public async Task<List<TopBusinessRaw>> GetTopBusinessesByRevenueAsync(int take)
    {
        var itemTotals = _context.SaleItems
            .Where(si => !si.IsDeleted)
            .GroupBy(si => si.SaleId)
            .Select(g => new { SaleId = g.Key, ItemsTotal = g.Sum(si => si.UnitPrice * si.Quantity) });

        var salesWithTotal = await (
            from s in _context.Sales
            where !s.IsDeleted
            join it in itemTotals on s.Id equals it.SaleId
            select new { s.BusinessId, Total = it.ItemsTotal - s.DiscountAmount }
        )
        .AsNoTracking()
        .ToListAsync();

        return salesWithTotal
            .GroupBy(x => x.BusinessId)
            .Select(g => new TopBusinessRaw(g.Key, g.Sum(x => x.Total)))
            .OrderByDescending(x => x.Revenue)
            .Take(take)
            .ToList();
    }

    public async Task<List<Business>> GetBusinessesByIdsAsync(IEnumerable<Guid> ids)
    {
        return await _context.Businesses
            .AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .ToListAsync();
    }

    public async Task<List<Notification>> GetRecentAdminActivityAsync(int take)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.Target == NotificationTarget.Admin)
            .OrderByDescending(n => n.SentAt)
            .Take(take)
            .ToListAsync();
    }
}