using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class PlatformReportController : CustomControllerBase
{
    private readonly IPlatformReportService _service;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PlatformReportController(
        IPlatformReportService service,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _service = service;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlatformReport([FromQuery] ReportPeriod period = ReportPeriod.Month)
    {
        var result = await _service.GetPlatformReportAsync(period);
        return Success(result, _localizer["PlatformReport.Get.Success"]);
    }
}