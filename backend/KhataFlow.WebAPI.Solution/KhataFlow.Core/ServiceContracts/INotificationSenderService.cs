using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface INotificationSenderService
{
    Task SendToUserAsync(Guid userId, NotificationResponse notification, CancellationToken ct = default);
    Task SendToBusinessAsync(Guid businessId, NotificationResponse notification, CancellationToken ct = default);
    Task SendToRoleAsync(string role, NotificationResponse notification, CancellationToken ct = default);
    Task SendToAllAsync(NotificationResponse notification, CancellationToken ct = default);}