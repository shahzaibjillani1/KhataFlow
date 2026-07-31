using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

[AllowAnonymous]
public class PublicLedgerController : CustomControllerBase
{
    private readonly ICustomerLedgerViewService _ledgerViewService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PublicLedgerController(
        ICustomerLedgerViewService ledgerViewService,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _ledgerViewService = ledgerViewService;
        _localizer = localizer;
    }

    [HttpGet("{token}")]
    [EnableRateLimiting("public-ledger")]
    public async Task<IActionResult> GetCustomerView(string token, CancellationToken ct)
    {
        var result = await _ledgerViewService.GetPublicLedgerViewAsync(token, ct);

        if (result is null)
            return NotFoundResponse(_localizer["PublicLedger.NotFound"]);

        return Success(result, _localizer["PublicLedger.Success"]);
    }
}