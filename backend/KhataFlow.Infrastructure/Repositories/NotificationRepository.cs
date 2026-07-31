using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Enums;
using KhataFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> AddNotificationAsync(Notification notification, CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
        await _context.SaveChangesAsync(ct);
        return notification;
    }

    public async Task<Notification?> GetNotificationByIdAsync(Guid notificationId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted, ct);
    }

    // A user should see: notifications addressed directly to them,
    // plus business-wide notifications for the business they belong to.
    public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => !n.IsDeleted &&
                ((n.Target == NotificationTarget.User && n.UserId == userId) ||
                 (n.Target == NotificationTarget.Business && n.BusinessId == businessId)))
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Notification>> GetUnreadForUserAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => !n.IsDeleted && !n.IsRead &&
                ((n.Target == NotificationTarget.User && n.UserId == userId) ||
                 (n.Target == NotificationTarget.Business && n.BusinessId == businessId)))
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .CountAsync(n => !n.IsDeleted && !n.IsRead &&
                ((n.Target == NotificationTarget.User && n.UserId == userId) ||
                 (n.Target == NotificationTarget.Business && n.BusinessId == businessId)), ct);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { notificationId }, ct);
        if (notification is null || notification.IsRead) return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        var unread = await _context.Notifications
            .Where(n => !n.IsDeleted && !n.IsRead &&
                ((n.Target == NotificationTarget.User && n.UserId == userId) ||
                 (n.Target == NotificationTarget.Business && n.BusinessId == businessId)))
            .ToListAsync(ct);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { notificationId }, ct);
        if (notification is null || notification.IsDeleted) return false;

        notification.IsDeleted = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return true;
    }
}