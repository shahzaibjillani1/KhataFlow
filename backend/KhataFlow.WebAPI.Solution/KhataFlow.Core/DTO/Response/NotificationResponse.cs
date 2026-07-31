using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleUr { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageUr { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public Guid? ReferenceId { get; set; }
}