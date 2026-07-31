using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface IReportService
{
    Task<FinancialSummaryResponse> GetFinancialSummaryAsync(Guid businessId, DateRange range);
    Task<decimal> GetGrossProfitAsync(Guid businessId, DateRange range);
    Task<decimal> GetTotalExpensesAsync(Guid businessId, DateRange range);
}