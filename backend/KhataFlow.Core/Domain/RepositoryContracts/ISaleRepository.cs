using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface ISaleRepository
{
    Task<Sale> AddAsync(Sale sale, Guid businessId);
    Task<List<Sale>> AddRangeAsync(IEnumerable<Sale> sales, Guid businessId);
    Task<int> CountSinceAsync(Guid businessId, DateTime since);
    Task<(List<Sale> Items, int TotalCount)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize);
    Task<Sale> UpdateAsync(Sale sale);                            
    Task<bool> DeleteAsync(Guid id);

    Task<Sale?> GetByIdAsync(Guid businessId, Guid saleId);
    Task<List<Sale>> GetByBusinessIdAsync(Guid businessId);

    Task<List<Sale>> GetByDateRangeAsync(Guid businessId, DateOnly from, DateOnly to); 
    Task<List<Sale>> GetTodaySalesAsync(Guid businessId);      
    Task<decimal> GetTotalRevenueAsync(Guid businessId, DateOnly from, DateOnly to);   
    Task<int> GetSaleCountAsync(Guid businessId);

    Task<Sale?> GetByProductNameAsync(string productName, Guid businessId);
    Task<decimal> GetTodaySalesTotalAsync(Guid businessId);
    Task<int> GetTodayOrderCountAsync(Guid businessId);
    Task<decimal> GetMonthlyRevenueAsync(Guid businessId);
    Task<List<WeeklySalesResponse>> GetWeeklySalesAsync(Guid businessId);

    Task<List<MonthlyRevenueResponse>> GetMonthlyRevenueAsync(Guid businessId, int year);
}