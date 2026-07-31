using Microsoft.EntityFrameworkCore;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Exceptions;
using KhataFlow.Infrastructure.Data;

namespace KhataFlow.Infrastructure.Repositories;

public class BusinessRepository : IBusinessRepository
{
    private readonly AppDbContext _context;

    public BusinessRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Business> AddAsync(Business business)
    {
        await _context.Businesses.AddAsync(business);
        await _context.SaveChangesAsync();
        return business;
    }

    public async Task<Business?> GetByIdAsync(Guid id)
    {
        return await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Business?> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.OwnerId == ownerId);
    }

    public async Task<List<Business>> GetAllAsync()
    {
        return await _context.Businesses
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Businesses.CountAsync();
    }

    public async Task<int> GetActiveSubscriptionsCountAsync()
    {
        return await _context.Businesses
            .CountAsync(b => b.Status == BusinessStatus.Active
                          && b.SubscriptionExpiry > DateTime.UtcNow);
    }

    public async Task<int> GetNewThisWeekCountAsync()
    {
        var weekStart = DateTime.UtcNow.Date
            .AddDays(-(int)DateTime.UtcNow.DayOfWeek);

        return await _context.Businesses
            .CountAsync(b => b.CreatedAt >= weekStart);
    }

    public async Task<bool> ExistsByOwnerIdAsync(Guid ownerId)
    {
        return await _context.Businesses
            .AnyAsync(b => b.OwnerId == ownerId);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Businesses
            .AnyAsync(b => b.Email == email);
    }

    public async Task<Business> UpdateAsync(Business business)
    {
        var existing = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == business.Id)
            ?? throw new NotFoundException($"Business '{business.Id}' not found.");

        if (!string.Equals(existing.Email, business.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await _context.Businesses
                .AnyAsync(b => b.Email == business.Email
                            && b.Id != business.Id);

            if (emailTaken)
                throw new ConflictException(
                    $"Email '{business.Email}' is already in use.");
        }

        existing.BusinessName = business.BusinessName;
        existing.BusinessNameUr = business.BusinessNameUr;
        existing.OwnerName = business.OwnerName;
        existing.OwnerNameUr = business.OwnerNameUr;
        existing.OwnerEmail = business.OwnerEmail;
        existing.Email = business.Email;
        existing.PhoneNumber = business.PhoneNumber;
        existing.Address = business.Address;
        existing.AddressUr = business.AddressUr;
        existing.SubscriptionPlan = business.SubscriptionPlan;
        existing.SubscriptionExpiry = business.SubscriptionExpiry;
        existing.Status = business.Status;
        existing.SuspensionReason = business.SuspensionReason;
        existing.SuspensionReasonUr = business.SuspensionReasonUr;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == id);

        if (business is null)
            return false;

        business.IsDeleted = true;
        business.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(List<Business>, int)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var query = _context.Businesses.AsQueryable();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}