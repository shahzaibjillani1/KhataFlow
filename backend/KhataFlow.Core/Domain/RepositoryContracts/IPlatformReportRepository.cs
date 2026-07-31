using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public record SaleRevenuePoint(DateTime Date, decimal Amount);
public record TopBusinessRaw(Guid BusinessId, decimal Revenue);

public interface IPlatformReportRepository
{
    Task<List<DateTime>> GetBusinessRegistrationDatesAsync(DateTime since);
    Task<List<DateTime>> GetUserRegistrationDatesAsync(DateTime since);
    Task<List<SaleRevenuePoint>> GetSaleRevenuePointsAsync(DateTime since);
    Task<List<TopBusinessRaw>> GetTopBusinessesByRevenueAsync(int take);
    Task<List<Business>> GetBusinessesByIdsAsync(IEnumerable<Guid> ids);
    Task<List<Notification>> GetRecentAdminActivityAsync(int take);
}