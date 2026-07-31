namespace KhataFlow.Core.DTO.Response;

public record PlatformSummaryResponse(
    int TotalUsers,        
    int ActiveSubscriptions,
    int NewThisWeek,
    decimal PlatformRevenue,   
    decimal TotalUserSales,       
    decimal ChurnRate,     
    decimal ARPU
);