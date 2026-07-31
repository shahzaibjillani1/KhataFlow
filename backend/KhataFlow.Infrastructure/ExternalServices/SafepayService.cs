using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KhataFlow.Core.Configuration;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KhataFlow.Infrastructure.ExternalServices;

public class SafepayService : IPaymentGatewayService
{
    private readonly HttpClient _http;
    private readonly SafepayOptions _options;
    private readonly ILogger<SafepayService> _logger;

    public SafepayService(HttpClient http, IOptions<SafepayOptions> options, ILogger<SafepayService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CreateHostedCheckoutAsync(PaymentGatewayRequest request, CancellationToken ct = default)
    {
        var trackerToken = await CreatePaymentSessionAsync(request, ct);
        var authToken = await CreateAuthTokenAsync(ct);

        var checkoutUrl =
            $"{_options.BaseUrl}/embedded/" +
            $"?tracker={Uri.EscapeDataString(trackerToken)}" +
            $"&tbt={Uri.EscapeDataString(authToken)}" +
            $"&environment={(_options.UseSandbox ? "sandbox" : "production")}" +
            $"&source=hosted" +
            $"&order_id={Uri.EscapeDataString(request.BasketId)}" +
            $"&redirect_url={Uri.EscapeDataString(request.SuccessUrl)}" +
            $"&cancel_url={Uri.EscapeDataString(request.FailureUrl)}";

        return checkoutUrl;
    }

    private async Task<string> CreatePaymentSessionAsync(PaymentGatewayRequest request, CancellationToken ct)
    {
        var minorUnitsAmount = (long)Math.Round(request.Amount * 100, MidpointRounding.AwayFromZero);

        var initBody = new
        {
            merchant_api_key = _options.PublicKey,
            intent = "CYBERSOURCE",
            mode = "payment",
            currency = "PKR",
            amount = minorUnitsAmount,
            metadata = new { order_id = request.BasketId },
        };

        using var initRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/order/payments/v3/")
        {
            Content = JsonContent.Create(initBody),
        };
        initRequest.Headers.Add("X-SFPY-MERCHANT-SECRET", _options.SecretKey);

        var initResponse = await _http.SendAsync(initRequest, ct);

        if (!initResponse.IsSuccessStatusCode)
        {
            var errorBody = await initResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Safepay session init failed: {StatusCode} {Body}",
                initResponse.StatusCode,
                errorBody);
            throw new AIServiceUnavailableException(
                $"Safepay rejected the session init ({(int)initResponse.StatusCode}): {errorBody}");
        }

        var initPayload = await initResponse.Content.ReadFromJsonAsync<SafepaySessionInitResponse>(cancellationToken: ct)
            ?? throw new AIServiceUnavailableException("Safepay session init response was empty.");

        return initPayload.data?.tracker?.token
            ?? throw new AIServiceUnavailableException(
                "Safepay session init response did not include data.tracker.token.");
    }

    private async Task<string> CreateAuthTokenAsync(CancellationToken ct)
    {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/client/passport/v1/token");
        
        tokenRequest.Headers.Add("X-SFPY-MERCHANT-SECRET", _options.SecretKey);

        var tokenResponse = await _http.SendAsync(tokenRequest, ct);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Safepay auth token request failed: {StatusCode} {Body}",
                tokenResponse.StatusCode,
                errorBody);
            throw new AIServiceUnavailableException(
                $"Safepay rejected the auth token request ({(int)tokenResponse.StatusCode}): {errorBody}");
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<SafepayAuthTokenResponse>(cancellationToken: ct)
            ?? throw new AIServiceUnavailableException("Safepay auth token response was empty.");

        return tokenPayload.data
            ?? throw new AIServiceUnavailableException("Safepay auth token response did not include a data field.");
    }

    private class SafepaySessionInitResponse
    {
        [JsonPropertyName("data")]
        public SafepaySessionData? data { get; set; }
    }

    private class SafepaySessionData
    {
        [JsonPropertyName("tracker")]
        public SafepayTracker? tracker { get; set; }
    }

    private class SafepayTracker
    {
        [JsonPropertyName("token")]
        public string? token { get; set; }
    }

    private class SafepayAuthTokenResponse
    {
        [JsonPropertyName("data")]
        public string? data { get; set; }
    }
}