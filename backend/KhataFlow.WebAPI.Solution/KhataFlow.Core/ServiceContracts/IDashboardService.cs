using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(Guid businessId);
}