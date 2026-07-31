using System.Text.Json;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace KhataFlow.Core.Services;

public class SubscriptionCheckoutService : ISubscriptionCheckoutService
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IIdempotencyRecordRepository _idempotencyRepository;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly SafepaySignatureVerifier _signatureVerifier;
    private readonly INotificationService _notificationService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionCheckoutService> _logger;

    private const decimal PremiumPlanMonthlyPrice = 999.00m;

    public SubscriptionCheckoutService(
        IBusinessRepository businessRepository,
        IIdempotencyRecordRepository idempotencyRepository,
        IPaymentGatewayService paymentGatewayService,
        SafepaySignatureVerifier signatureVerifier,
        INotificationService notificationService,
        IStringLocalizer<SharedResource> localizer,
        IConfiguration config,
        ILogger<SubscriptionCheckoutService> logger
    )
    {
        _businessRepository = businessRepository;
        _idempotencyRepository = idempotencyRepository;
        _paymentGatewayService = paymentGatewayService;
        _signatureVerifier = signatureVerifier;
        _notificationService = notificationService;
        _localizer = localizer;
        _config = config;
        _logger = logger;
    }

    public async Task<SubscriptionCheckoutResponse> StartCheckoutAsync(
        Guid businessId,
        CancellationToken ct = default
    )
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException(
                _localizer["Subscription.BusinessId.Empty"],
                nameof(businessId)
            );

        var business =
            await _businessRepository.GetByIdAsync(businessId)
            ?? throw new KeyNotFoundException(
                _localizer["Subscription.Business.NotFound", businessId]
            );

        var basketId = $"SUB-{business.Id:N}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var frontendBaseUrl = _config["FrontendBaseUrl"]!.TrimEnd('/');

        
        var checkoutUrl = await _paymentGatewayService.CreateHostedCheckoutAsync(
            new PaymentGatewayRequest(
                Amount: PremiumPlanMonthlyPrice,
                BasketId: basketId,
                CustomerMobile: business.PhoneNumber,
                CustomerEmail: business.Email,
                SuccessUrl: $"{frontendBaseUrl}/settings?payment=success&order={basketId}",
                FailureUrl: $"{frontendBaseUrl}/settings?payment=failed&order={basketId}"
            ),
            ct
        );

        return new SubscriptionCheckoutResponse(checkoutUrl);
    }

    public async Task<bool> ProcessWebhookAsync(
    string rawBody,
    string signatureHeader,
    CancellationToken ct = default
)
    {
        // TEMPORARY — remove once this is confirmed working. Logs the exact payload
        // Safepay sends so we can stop guessing at field names/casing.
        _logger.LogInformation("Safepay webhook raw body: {RawBody}", rawBody);

        if (!_signatureVerifier.IsValid(rawBody, signatureHeader))
        {
            _logger.LogWarning("Safepay webhook signature verification failed.");
            return false;
        }

        var payload = JsonSerializer.Deserialize<SafepayWebhookRequest>(
            rawBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (payload is null)
        {
            _logger.LogWarning("Safepay webhook body could not be parsed.");
            return true;
        }

        if (string.IsNullOrEmpty(payload.Token))
        {
            _logger.LogWarning("Safepay webhook payload had no token — skipping idempotency check and processing.");
        }
        else if (await _idempotencyRepository.ExistsAsync(payload.Token))
        {
            _logger.LogInformation("Safepay webhook token {Token} already processed — skipping.", payload.Token);
            return true;
        }

        // Case/whitespace-tolerant — was an exact string match before, which silently
        // no-ops on any casing difference from what Safepay actually sends.
        var eventType = payload.Type?.Trim();
        if (!string.Equals(eventType, "payment.succeeded", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Safepay webhook type {Type} is not a success event — skipping.", payload.Type);
            return true;
        }

        // Dictionary<string,string> keys are case-sensitive by default — rebuild with a
        // case-insensitive comparer so "order_id" still matches "Order_Id"/"OrderId" etc.
        var metadata = payload.Data.Metadata is not null
            ? new Dictionary<string, string>(payload.Data.Metadata, StringComparer.OrdinalIgnoreCase)
            : null;

        var orderId = metadata is not null && metadata.TryGetValue("order_id", out var oid) ? oid : null;

        var businessId = orderId is not null ? ExtractBusinessId(orderId) : null;
        if (businessId is null)
        {
            _logger.LogWarning(
                "Could not parse businessId from webhook metadata.order_id: {OrderId}. Full metadata: {Metadata}",
                orderId,
                metadata is null ? "null" : JsonSerializer.Serialize(metadata));
            return true;
        }

        var business = await _businessRepository.GetByIdAsync(businessId.Value);
        if (business is null)
        {
            _logger.LogWarning("Webhook resolved businessId {BusinessId} but no matching business found.", businessId);
            return true;
        }

        var renewsAt = DateTime.UtcNow.AddMonths(1);
        business.SubscriptionPlan = SubscriptionPlanType.Premium;
        business.SubscriptionRenewsAt = renewsAt;
        business.SubscriptionExpiry = renewsAt; // Settings page reads this field — was never being set, so plan stayed "Premium" internally but any expiry-based checks/display were stale
        await _businessRepository.UpdateAsync(business);

        _logger.LogInformation("Business {BusinessId} upgraded to Premium via webhook token {Token}.", business.Id, payload.Token);

        await TryNotifyAsync(
            new CreateNotificationRequest(
                Target: NotificationTarget.Business,
                Title: _localizer["Subscription.Notification.Upgraded.Title"],
                Message: string.Format(
                    _localizer["Subscription.Notification.Upgraded.Message"],
                    business.BusinessName
                ),
                Type: NotificationType.PlanCreated,
                BusinessId: business.Id,
                ReferenceId: business.Id
            )
        );

        if (!string.IsNullOrEmpty(payload.Token))
        {
            await _idempotencyRepository.AddAsync(
                new IdempotencyRecord { IdempotencyKey = payload.Token, CreatedAt = DateTime.UtcNow }
            );
        }

        return true;
    }

    private static Guid? ExtractBusinessId(string basketId)
    {
        var parts = basketId.Split('-');
        return parts.Length >= 2 && Guid.TryParse(parts[1], out var id) ? id : null;
    }

    private async Task TryNotifyAsync(CreateNotificationRequest request)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(request);
        }
        catch
        {
            
        }
    }
}