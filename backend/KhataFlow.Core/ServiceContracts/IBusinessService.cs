using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.ServiceContracts;

public interface IBusinessService
{
    Task<List<BusinessResponse>> GetAllBusinessesAsync();
    Task<BusinessResponse?> GetBusinessByIdAsync(Guid id);
    Task<BusinessResponse?> GetMyBusinessAsync(Guid id);
    Task<PaginatedResponse<BusinessResponse>> GetBusinessesPagedAsync(int pageNumber, int pageSize);
    Task<BusinessResponse> AddBusinessAsync(BusinessAddRequest request, Guid userId);
    Task<BusinessResponse> UpdateBusinessAsync(BusinessUpdateRequest request, Guid userId);
    Task<bool> DeleteBusinessAsync(Guid id, Guid userId);

    Task<bool> SuspendBusinessAsync(Guid businessId, string reason);
    Task<bool> ReactivateBusinessAsync(Guid businessId);
    Task<ImpersonationTokenResponse> LoginAsBusinessAsync(Guid businessId);  
    Task<SubscriptionResponse> UpgradePlanAsync(Guid businessId, SubscriptionPlanType newPlan);
    Task<SubscriptionResponse> RenewSubscriptionAsync(Guid businessId);
    Task<SubscriptionResponse> ChangeSubscriptionAsync(Guid businessId, ChangeSubscriptionRequest request);

    Task<PlatformSummaryResponse> GetPlatformSummaryAsync();
}