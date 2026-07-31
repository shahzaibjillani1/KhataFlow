using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace KhataFlow.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        var businessId = Context.User?.FindFirst("businessId")?.Value;
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        if (!string.IsNullOrEmpty(businessId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"business-{businessId}");

        if (!string.IsNullOrEmpty(role))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");

        await base.OnConnectedAsync();
    }
}