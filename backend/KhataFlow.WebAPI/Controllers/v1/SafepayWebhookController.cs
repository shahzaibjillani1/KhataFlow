using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

[AllowAnonymous]
public class SafepayWebhookController : CustomControllerBase
{
    private readonly ISubscriptionCheckoutService _checkoutService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SafepayWebhookController(
        ISubscriptionCheckoutService checkoutService,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _checkoutService = checkoutService;
        _localizer = localizer;
    }

    // Register this endpoint's public URL under Developers > Endpoints in the Safepay
    // sandbox dashboard — that's also where the Webhook Secret (SafepayOptions.WebhookSecret)
    // comes from.
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        if (!Request.Headers.TryGetValue("X-SFPY-SIGNATURE", out var signature))
        {
            return Unauthorized();
        }

        var processed = await _checkoutService.ProcessWebhookAsync(rawBody, signature!, HttpContext.RequestAborted);

        return processed ? Ok() : Unauthorized();
    }
}