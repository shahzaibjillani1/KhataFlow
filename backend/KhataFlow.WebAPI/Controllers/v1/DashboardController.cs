using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

[Authorize]
public class DashboardController : CustomControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DashboardController(
        IDashboardService dashboardService,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _dashboardService = dashboardService;
        _localizer = localizer;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var businessId = GetBusinessId();
        var summary = await _dashboardService.GetDashboardSummaryAsync(businessId);

        return Success(summary, _localizer["Dashboard.Summary.Success"]);
    }
}