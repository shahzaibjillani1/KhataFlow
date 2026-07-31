using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ISubscriptionCheckoutService
{
    Task<SubscriptionCheckoutResponse> StartCheckoutAsync(Guid businessId, CancellationToken ct = default);

    Task<bool> ProcessWebhookAsync(
        string rawBody,
        string signatureHeader,
        CancellationToken ct = default
    );
}