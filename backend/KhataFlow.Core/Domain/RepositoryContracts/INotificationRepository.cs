using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface INotificationRepository
{
    Task<Notification> AddNotificationAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetNotificationByIdAsync(Guid notificationId, CancellationToken ct = default);
    Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task<IEnumerable<Notification>> GetUnreadForUserAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, Guid businessId, CancellationToken ct = default);
    Task<bool> DeleteNotificationAsync(Guid notificationId, CancellationToken ct = default);
}