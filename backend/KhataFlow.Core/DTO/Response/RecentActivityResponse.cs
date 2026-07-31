namespace KhataFlow.Core.DTO.Response;

public record RecentActivityResponse(
    Guid Id,
    string Message,
    string Type,
    DateTime Timestamp
);
