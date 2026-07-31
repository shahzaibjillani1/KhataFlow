using KhataFlow.Core.Enums;

namespace KhataFlow.Core.ServiceContracts;

public interface IPlanLimitService
{
    Task EnsureCanCreateSaleAsync(Guid businessId);
    Task EnsureCanAddProductAsync(Guid businessId);
    Task EnsureCanAddCustomerAsync(Guid businessId);
    Task EnsureCanAddStaffAsync(Guid businessId);
    Task EnsureFeatureEnabledAsync(Guid businessId, PlanFeature feature);
}