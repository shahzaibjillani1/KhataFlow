using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ISaleService
{
    Task<List<SaleResponse>> GetAllSalesAsync(Guid businessId);

    Task<SaleResponse?> GetSaleByIdAsync(Guid saleId, Guid businessId);
    Task<PaginatedResponse<SaleResponse>> GetSalesPagedAsync(Guid businessId, int pageNumber, int pageSize);

    Task<SaleResponse> AddSaleAsync(SaleAddRequest saleAddRequest, Guid businessId);

    Task<List<SaleResponse>> AddSalesAsync(IEnumerable<SaleAddRequest> saleAddRequests, Guid businessId);
    Task<SaleResponse> UpdateSaleAsync(Guid businessId, Guid saleId, SaleUpdateRequest request);

    Task<bool> DeleteSaleAsync(Guid businessId, Guid saleId);

    Task<List<SaleResponse>> GetTodaySalesAsync(Guid businessId);     

    Task<decimal> GetMonthlyRevenueAsync(Guid businessId);

    Task<int> GetTotalOrdersAsync(Guid businessId); 


    Task<SaleResponse> GetSaleByProductNameAsync(string productName, Guid businessId);


    Task<List<MonthlyRevenueResponse>> GetMonthlyRevenueAsync(Guid businessId, int year);

    Task<List<WeeklySalesResponse>> GetWeeklySalesAsync(Guid businessId);
}