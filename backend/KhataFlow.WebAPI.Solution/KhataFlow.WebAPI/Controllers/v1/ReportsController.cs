using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class ReportsController : CustomControllerBase
{
    private readonly IReportService _reportService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReportsController(
        IReportService reportService,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _reportService = reportService;
        _localizer = localizer;
    }

    [HttpGet("financial-report")]
    public async Task<IActionResult> GetFinancialReport(DateOnly from, DateOnly to)
    {
        var range = new DateRange(from, to);
        var businessId = GetBusinessId();
        var report = await _reportService.GetFinancialSummaryAsync(businessId, range);
        return Success(report, _localizer["Reports.Financial.Success"]);
    }

    [HttpGet("gross-profit")]
    public async Task<IActionResult> GetGrossProfit(DateOnly from, DateOnly to)
    {
        var range = new DateRange(from, to);
        var businessId = GetBusinessId();
        var grossProfit = await _reportService.GetGrossProfitAsync(businessId, range);
        return Success(grossProfit, _localizer["Reports.GrossProfit.Success"]);
    }

    [HttpGet("total-expenses")]
    public async Task<IActionResult> GetTotalExpenses(DateOnly from, DateOnly to)
    {
        var range = new DateRange(from, to);
        var businessId = GetBusinessId();
        var totalExpenses = await _reportService.GetTotalExpensesAsync(businessId, range);
        return Success(totalExpenses, _localizer["Reports.TotalExpenses.Success"]);
    }
}