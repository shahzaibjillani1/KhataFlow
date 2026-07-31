namespace KhataFlow.Core.DTO.Response;

public record GrowthReportResponse(
    List<string> Labels,
    List<int> Users,
    List<int> Businesses,
    List<decimal> Revenue
);