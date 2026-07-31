using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Mappers;
using KhataFlow.Core.ServiceContracts;

namespace KhataFlow.Core.Services;

public class PlatformReportService : IPlatformReportService
{
    private readonly IPlatformReportRepository _repository;
    private readonly ISubscriptionPlanService _subscriptionPlanService;

    private const int TopBusinessesCount = 10;
    private const int RecentActivityCount = 20;

    private record Bucket(DateTime Start, DateTime End, string Label);

    public PlatformReportService(
        IPlatformReportRepository repository,
        ISubscriptionPlanService subscriptionPlanService)
    {
        _repository = repository;
        _subscriptionPlanService = subscriptionPlanService;
    }

    public async Task<PlatformReportResponse> GetPlatformReportAsync(ReportPeriod period)
    {
        var buckets = BuildBuckets(period);
        var since = buckets[0].Start;

        var businessDates = await _repository.GetBusinessRegistrationDatesAsync(since);
        var userDates = await _repository.GetUserRegistrationDatesAsync(since);
        var revenuePoints = await _repository.GetSaleRevenuePointsAsync(since);
        var topBusinessesRaw = await _repository.GetTopBusinessesByRevenueAsync(TopBusinessesCount);
        var recentActivityRaw = await _repository.GetRecentAdminActivityAsync(RecentActivityCount);
        var plans = await _subscriptionPlanService.GetAllPlansAsync();

        var growth = BuildGrowthReport(buckets, businessDates, userDates, revenuePoints);
        var revenueByPlan = BuildRevenueByPlan(plans);
        var topBusinesses = await BuildTopBusinessesAsync(topBusinessesRaw);
        var recentActivity = recentActivityRaw.Select(PlatformReportMapper.ToActivityResponse).ToList();

        return new PlatformReportResponse(growth, revenueByPlan, topBusinesses, recentActivity);
    }

    private static List<Bucket> BuildBuckets(ReportPeriod period)
    {
        var buckets = new List<Bucket>();
        var now = DateTime.UtcNow;

        if (period == ReportPeriod.Week)
        {
            for (var i = 6; i >= 0; i--)
            {
                var day = now.Date.AddDays(-i);
                buckets.Add(new Bucket(day, day.AddDays(1), day.ToString("ddd")));
            }
        }
        else
        {
            for (var i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                buckets.Add(new Bucket(monthStart, monthStart.AddMonths(1), monthStart.ToString("MMM")));
            }
        }

        return buckets;
    }

    private static GrowthReportResponse BuildGrowthReport(
        List<Bucket> buckets,
        List<DateTime> businessDates,
        List<DateTime> userDates,
        List<Domain.RepositoryContracts.SaleRevenuePoint> revenuePoints)
    {
        var labels = buckets.Select(b => b.Label).ToList();
        var businesses = buckets.Select(b => businessDates.Count(d => d >= b.Start && d < b.End)).ToList();
        var users = buckets.Select(b => userDates.Count(d => d >= b.Start && d < b.End)).ToList();
        var revenue = buckets
            .Select(b => revenuePoints.Where(p => p.Date >= b.Start && p.Date < b.End).Sum(p => p.Amount))
            .ToList();

        return new GrowthReportResponse(labels, users, businesses, revenue);
    }

    private static List<PlanRevenueBreakdownResponse> BuildRevenueByPlan(
        List<DTO.Response.SubscriptionPlanResponse> plans)
    {
        
        var totalRevenue = plans.Sum(p => p.TotalRevenue);

        return plans
            .Select(p => new PlanRevenueBreakdownResponse(
                p.Id,
                p.PlanName,
                p.TotalRevenue,
                totalRevenue > 0 ? (double)(p.TotalRevenue / totalRevenue) * 100 : 0
            ))
            .ToList();
    }

    private async Task<List<TopBusinessResponse>> BuildTopBusinessesAsync(
        List<Domain.RepositoryContracts.TopBusinessRaw> raw)
    {
        if (raw.Count == 0) return [];

        var businesses = await _repository.GetBusinessesByIdsAsync(raw.Select(r => r.BusinessId));
        var businessMap = businesses.ToDictionary(b => b.Id);
        var maxRevenue = raw.Max(r => r.Revenue);

        return raw
            .Select(r =>
            {
                businessMap.TryGetValue(r.BusinessId, out var business);
                return new TopBusinessResponse(
                    r.BusinessId,
                    business?.BusinessName ?? "Unknown",
                    business?.BusinessNameUr ?? "Unknown",
                    r.Revenue,
                    business is null ? "Unknown" : PlanLabel(business.SubscriptionPlan),
                    maxRevenue > 0 ? (double)(r.Revenue / maxRevenue) * 100 : 0
                );
            })
            .ToList();
    }

    private static string PlanLabel(SubscriptionPlanType plan) => plan switch
    {
        SubscriptionPlanType.Free => "Free",
        SubscriptionPlanType.Premium => "Premium",
        _ => "Unknown",
    };
}