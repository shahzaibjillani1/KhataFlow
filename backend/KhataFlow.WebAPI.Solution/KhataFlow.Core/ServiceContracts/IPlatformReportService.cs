using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.ServiceContracts;

public interface IPlatformReportService
{
    Task<PlatformReportResponse> GetPlatformReportAsync(ReportPeriod period);
}