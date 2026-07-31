using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mappers;

public static class PlatformReportMapper
{
    public static RecentActivityResponse ToActivityResponse(Notification n) =>
        new(n.Id, n.Message, n.Type.ToString(), n.SentAt);
}