using System.Text.Encodings.Web;

namespace KhataFlow.WebAPI.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddConfiguredSignalR(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            });

        return services;
    }
}