using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class LedgerController : CustomControllerBase
{
    private readonly ILedgerService _ledgerService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public LedgerController(
        ILedgerService ledgerService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _ledgerService = ledgerService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet]
    public async Task<IActionResult> GetKhata(
    Guid customerId,
    [FromQuery] DateTime? before = null,
    [FromQuery] int limit = 200)
    {
        var businessId = GetBusinessId();
        var khata = await _ledgerService.GetKhataAsync(businessId, customerId, before, Math.Clamp(limit, 1, 200));

        return Success(khata, _localizer["Ledger.GetKhata.Success"]);
    }

    [HttpPost("udhar")]
    public async Task<IActionResult> AddUdhar(Guid customerId, [FromBody] AddUdharRequest request)
    {
        if (customerId != request.CustomerId)
            return BadRequestResponse(_localizer["Ledger.CustomerId.Mismatch"]);

        var businessId = GetBusinessId();

        try
        {
            var entry = await _ledgerService.AddUdharAsync(businessId, request);
            return Success(entry, _localizer["Ledger.AddUdhar.Success"]);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpPost("payment")]
    public async Task<IActionResult> RecordPayment(Guid customerId, [FromBody] RecordPaymentRequest request)
    {
        if (customerId != request.CustomerId)
            return BadRequestResponse(_localizer["Ledger.CustomerId.Mismatch"]);

        var businessId = GetBusinessId();

        try
        {
            var entry = await _ledgerService.RecordPaymentAsync(businessId, request);
            return Success(entry, _localizer["Ledger.RecordPayment.Success"]);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(await TranslateDynamicAsync(ex.Message));
        }
    }
}