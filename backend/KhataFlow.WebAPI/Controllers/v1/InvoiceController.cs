using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

[Authorize]
public class InvoiceController : CustomControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public InvoiceController(
        IInvoiceService invoiceService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _invoiceService = invoiceService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet("{saleId}")]
    public async Task<IActionResult> GetInvoice(Guid saleId)
    {
        var businessId = GetBusinessId();

        try
        {
            var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(businessId, saleId);
            return File(pdfBytes, "application/pdf", $"invoice-{saleId}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Invoice.Sale.NotFound", saleId]);
        }
    }

    [HttpGet("{saleId}/print")]
    public async Task<IActionResult> PrintInvoice(Guid saleId)
    {
        var businessId = GetBusinessId();

        try
        {
            var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(businessId, saleId);
            Response.Headers.Append("Content-Disposition", $"inline; filename=invoice-{saleId}.pdf");
            return File(pdfBytes, "application/pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Invoice.Sale.NotFound", saleId]);
        }
    }
}