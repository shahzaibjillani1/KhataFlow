using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.ServiceContracts;
using KhataFlow.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KhataFlow.WebAPI.Services;

public class NotificationSenderService : INotificationSenderService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationSenderService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task SendToUserAsync(Guid userId, NotificationResponse notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendToBusinessAsync(Guid businessId, NotificationResponse notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"business-{businessId}").SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendToRoleAsync(string role, NotificationResponse notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"role-{role}").SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendToAllAsync(NotificationResponse notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, ct);
    }
}
