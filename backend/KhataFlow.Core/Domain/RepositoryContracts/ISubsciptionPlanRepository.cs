using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface ISubscriptionPlanRepository
{
    Task<List<SubscriptionPlan>> GetAllAsync();

    Task<SubscriptionPlan?> GetByIdAsync(Guid id);
    Task<SubscriptionPlan?> GetByPlanTypeAsync(SubscriptionPlanType planType);

    Task<SubscriptionPlan> AddAsync(SubscriptionPlan plan);

    Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan);             

    Task<bool> DeleteAsync(Guid id);

    Task<bool> ExistsAsync(string planName);
    Task<int> GetUserCountByPlanAsync(Guid planId);     
    Task<decimal> GetRevenueByPlanAsync(Guid planId);
}