using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface INotificationService
{
    Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken ct = default);
    Task<NotificationResponse> GetNotificationByIdAsync(Guid notificationId, CancellationToken ct = default);
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task<IEnumerable<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task<bool> DeleteNotificationAsync(Guid notificationId, CancellationToken ct = default);
}