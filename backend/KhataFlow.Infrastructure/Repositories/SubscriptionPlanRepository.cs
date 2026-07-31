using Microsoft.EntityFrameworkCore;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Enums;
using KhataFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using KhataFlow.Core.Domain.IdentityEntities;

namespace KhataFlow.Infrastructure.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    public SubscriptionPlanRepository(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<SubscriptionPlan> AddAsync(SubscriptionPlan plan)
    {
        await _context.SubscriptionPlans.AddAsync(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);

        if (plan == null || plan.IsDeleted)
            return false;

        plan.IsDeleted = true;
        plan.IsActive = false;   
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string planName)
    {
        return await _context.SubscriptionPlans
            .AnyAsync(sp =>
                sp.PlanName == planName &&
                !sp.IsDeleted);
    }

    public async Task<List<SubscriptionPlan>> GetAllAsync()
    {
        return await _context.SubscriptionPlans
            .Where(sp => !sp.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id)
    {
        return await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted);
    }

    public async Task<SubscriptionPlan?> GetByPlanTypeAsync(SubscriptionPlanType planType)
    => await _context.SubscriptionPlans
        .FirstOrDefaultAsync(sp => sp.PlanType == planType && sp.IsActive);

    public async Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan)
    {
        var existing = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(sp => sp.Id == plan.Id && !sp.IsDeleted)
            ?? throw new KeyNotFoundException($"SubscriptionPlan '{plan.Id}' not found.");

        existing.PlanName = plan.PlanName;
        existing.PlanNameUr = plan.PlanNameUr;
        existing.MonthlyPrice = plan.MonthlyPrice;
        existing.Features = plan.Features;
        existing.FeaturesUr = plan.FeaturesUr;
        existing.PlanType = plan.PlanType;
        existing.IsActive = plan.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<int> GetUserCountByPlanAsync(Guid planId)
    {
        var plan = await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == planId && !sp.IsDeleted)
            ?? throw new KeyNotFoundException($"Plan '{planId}' not found.");

        return await _userManager.Users
            .Where(u => u.Plan == plan.PlanType)
            .CountAsync();
    }

    public async Task<decimal> GetRevenueByPlanAsync(Guid planId)
    {
        var plan = await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == planId && !sp.IsDeleted)
            ?? throw new KeyNotFoundException($"Plan '{planId}' not found.");

        var userCount = await GetUserCountByPlanAsync(planId);

        return plan.MonthlyPrice * userCount;
    }
}