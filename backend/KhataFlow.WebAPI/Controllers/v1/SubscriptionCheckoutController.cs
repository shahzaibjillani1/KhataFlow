using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class SubscriptionCheckoutController : CustomControllerBase
{
    private readonly ISubscriptionCheckoutService _checkoutService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SubscriptionCheckoutController(
        ISubscriptionCheckoutService checkoutService,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _checkoutService = checkoutService;
        _localizer = localizer;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> StartCheckout()
    {
        var businessId = GetBusinessId();

        try
        {
            var data = await _checkoutService.StartCheckoutAsync(businessId);
            return Success(data, _localizer["Subscription.Checkout.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Subscription.Business.NotFound"]);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }
}