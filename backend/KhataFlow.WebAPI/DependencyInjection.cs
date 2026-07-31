using KhataFlow.Core.Enums;
using KhataFlow.Core.ServiceContracts;
using KhataFlow.WebAPI.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace KhataFlow.WebAPI;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationSenderService, NotificationSenderService>();
          services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("public-ledger", opt =>
            {
                opt.PermitLimit = 20;
                opt.Window = TimeSpan.FromMinutes(1);
            });
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("OwnerOnly", p =>
                p.RequireClaim("role", nameof(UserRole.Owner)));

            options.AddPolicy("OwnerOrManager", p =>
                p.RequireClaim("role", nameof(UserRole.Owner), nameof(UserRole.Manager)));

            options.AddPolicy("AnyBusinessUser", p =>
                p.RequireClaim("role", nameof(UserRole.Owner), nameof(UserRole.Manager), nameof(UserRole.Staff)));

            options.AddPolicy("SuperAdminOnly", p =>
                p.RequireClaim("role", nameof(UserRole.SuperAdmin)));
        });

        return services;
    }
}
