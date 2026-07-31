using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.ServiceContracts;

namespace KhataFlow.Core.Services;

public class DashboardService : IDashboardService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILedgerRepository _ledgerRepository;

    public DashboardService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        ILedgerRepository ledgerRepository)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _ledgerRepository = ledgerRepository;
    }

    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(Guid businessId)
    {
        decimal todaySales = await _saleRepository.GetTodaySalesTotalAsync(businessId);
        int todayOrders = await _saleRepository.GetTodayOrderCountAsync(businessId);
        decimal monthlyRevenue = await _saleRepository.GetMonthlyRevenueAsync(businessId);

        int customers = await _customerRepository.GetCustomerCountAsync(businessId);
        int products = await _productRepository.GetProductCountAsync(businessId);
        int lowStock = await _productRepository.GetLowStockCountAsync(businessId);

        // FIXED: was calling the per-customer overload with a businessId — wrong data, no compile error.
        decimal outstandingBalance = await _ledgerRepository.GetOutstandingBalanceForBusinessAsync(businessId);

        var monthRange = new DateRange(
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day)),
            DateOnly.FromDateTime(DateTime.UtcNow.Date));

        decimal monthlyExpenses = await _ledgerRepository.GetExpensesInRangeAsync(businessId, monthRange.From, monthRange.To);
        decimal profit = monthlyRevenue - monthlyExpenses;

        return new DashboardSummaryResponse
        {
            TodaySales = todaySales,
            MonthlyRevenue = monthlyRevenue,
            Profit = profit,
            Customers = customers,
            Products = products,
            LowStock = lowStock,
            OutstandingBalance = outstandingBalance,
            TodayOrders = todayOrders
        };
    }
}