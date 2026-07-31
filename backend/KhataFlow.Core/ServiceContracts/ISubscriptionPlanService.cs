using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ISubscriptionPlanService
{
    Task<List<SubscriptionPlanResponse>> GetAllPlansAsync();

    Task<SubscriptionPlanResponse?> GetPlanByIdAsync(Guid id);

    Task<SubscriptionPlanResponse> AddPlanAsync(SubscriptionPlanAddRequest request);

    Task<SubscriptionPlanResponse> UpdatePlanAsync(SubscriptionPlanUpdateRequest request);  

    Task<bool> DeletePlanAsync(Guid id);

    Task<int> GetUserCountByPlanAsync(Guid planId); 

    Task<decimal> GetRevenueByPlanAsync(Guid planId);  
}