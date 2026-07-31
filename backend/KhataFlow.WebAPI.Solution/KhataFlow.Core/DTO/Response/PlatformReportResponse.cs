namespace KhataFlow.Core.DTO.Response;

public record PlatformReportResponse(
    GrowthReportResponse Growth,
    List<PlanRevenueBreakdownResponse> RevenueByPlan,
    List<TopBusinessResponse> TopBusinesses,
    List<RecentActivityResponse> RecentActivity
);
