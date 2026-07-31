using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record CreateNotificationRequest(
    NotificationTarget Target,
    string Title,
    string Message,
    NotificationType Type,
    Guid? UserId = null,
    Guid? BusinessId = null,
    Guid? ReferenceId = null
    );