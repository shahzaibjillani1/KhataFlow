using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class NotificationController : CustomControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public NotificationController(
        INotificationService notificationService,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _notificationService = notificationService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var businessId = GetBusinessId();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, businessId, ct);

        return Success(notifications, _localizer["Notification.GetAll.Success"]);
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var businessId = GetBusinessId();

        var notifications = await _notificationService.GetUnreadNotificationsAsync(userId, businessId, ct);

        return Success(notifications, _localizer["Notification.GetUnread.Success"]);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var businessId = GetBusinessId();

        var count = await _notificationService.GetUnreadCountAsync(userId, businessId, ct);

        return Success(count, _localizer["Notification.GetUnreadCount.Success"]);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id, ct);

        return Success(notification, _localizer["Notification.GetById.Success"]);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        await _notificationService.MarkAsReadAsync(id, ct);

        return NoContentResponse(_localizer["Notification.MarkAsRead.Success"]);
    }

    [HttpPatch("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var businessId = GetBusinessId();

        await _notificationService.MarkAllAsReadAsync(userId, businessId, ct);

        return NoContentResponse(_localizer["Notification.MarkAllAsRead.Success"]);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _notificationService.DeleteNotificationAsync(id, ct);

        if (!deleted)
            return NotFoundResponse(_localizer["Notification.NotFound", id]);

        return NoContentResponse(_localizer["Notification.Delete.Success"]);
    }
}