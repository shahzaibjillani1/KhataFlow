using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class InvoiceSettingsController : CustomControllerBase
{
    private readonly IInvoiceSettingsService _invoiceSettingsService;
    private readonly ISaleService _saleService;
    private readonly IInvoiceDocumentBuilder _documentBuilder;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public InvoiceSettingsController(
        IInvoiceSettingsService invoiceSettingsService,
        ISaleService saleService,
        IInvoiceDocumentBuilder documentBuilder,
        IStringLocalizer<SharedResource> localizer
    )
        : base(localizer)
    {
        _invoiceSettingsService = invoiceSettingsService;
        _saleService = saleService;
        _documentBuilder = documentBuilder;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var businessId = GetBusinessId();
        var settings = await _invoiceSettingsService.GetAsync(businessId);
        return Success(settings, _localizer["InvoiceSettings.Get.Success"]);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] InvoiceSettingsRequest request)
    {
        var businessId = GetBusinessId();

        try
        {
            var updated = await _invoiceSettingsService.UpdateAsync(request, businessId);
            return Success(updated, _localizer["InvoiceSettings.Update.Success"]);
        }
        catch (PlanLimitExceededException ex)
        {
            return ConflictResponse(ex.Message);
        }
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewInvoice([FromBody] InvoiceSettingsRequest request)
    {
        var businessId = GetBusinessId();
        var sales = await _saleService.GetAllSalesAsync(businessId);
        var latestSale = sales.OrderByDescending(s => s.Date).FirstOrDefault();

        if (latestSale is null)
            return NotFoundResponse(_localizer["InvoiceSettings.Preview.NoSalesAvailable"]);

        throw new NotImplementedException(
            "Wire to ISaleRepository.GetByIdAsync + IBusinessRepository to get entities for the PDF builder."
        );
    }
}
