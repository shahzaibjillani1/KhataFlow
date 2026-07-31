using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class SalesController : CustomControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public SalesController(
        ISaleService saleService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _saleService = saleService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSales([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var businessId = GetBusinessId();
        var result = await _saleService.GetSalesPagedAsync(businessId, pageNumber, pageSize);
        return Success(result, _localizer["Sale.GetAll.Success"]);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSaleById(Guid id)
    {
        var businessId = GetBusinessId();

        try
        {
            var sale = await _saleService.GetSaleByIdAsync(businessId, id);
            return Success(sale, _localizer["Sale.GetById.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Sale.NotFound", id]);
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchSales([FromQuery] string query)
    {
        var businessId = GetBusinessId();
        var sale = await _saleService.GetSaleByProductNameAsync(query, businessId);
        return Success(sale, _localizer["Sale.Search.Success"]);
    }

    [HttpGet("today-sales")]
    public async Task<IActionResult> GetTodaySales()
    {
        var businessId = GetBusinessId();
        var sales = await _saleService.GetTodaySalesAsync(businessId);
        return Success(sales, _localizer["Sale.Today.Success"]);
    }

    [HttpGet("total-monthly-revenue")]
    public async Task<IActionResult> GetMonthlyRevenue()
    {
        var businessId = GetBusinessId();
        decimal sale = await _saleService.GetMonthlyRevenueAsync(businessId);
        return Success(sale, _localizer["Sale.TotalMonthlyRevenue.Success"]);
    }

    [HttpGet("total-sales")]
    public async Task<IActionResult> GetTotalSales()
    {
        var businessId = GetBusinessId();
        decimal totalSales = await _saleService.GetTotalOrdersAsync(businessId);
        return Success(totalSales, _localizer["Sale.TotalSales.Success"]);
    }

    [HttpPost]
    public async Task<IActionResult> AddSale([FromBody] SaleAddRequest request)
    {
        var businessId = GetBusinessId();

        try
        {
            var sale = await _saleService.AddSaleAsync(request, businessId);
            return Created(sale, _localizer["Sale.Create.Success"]);
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

    [HttpPost("bulk")]
    public async Task<IActionResult> AddSales([FromBody] IEnumerable<SaleAddRequest> requests)
    {
        var businessId = GetBusinessId();

        try
        {
            var sales = await _saleService.AddSalesAsync(requests, businessId);
            return Success(sales, _localizer["Sale.CreateBulk.Success"]);
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSale(Guid id)
    {
        var businessId = GetBusinessId();
        var deleted = await _saleService.DeleteSaleAsync(businessId, id);
        return Success(deleted, _localizer["Sale.Delete.Success"]);
    }

    [HttpGet("weekly-sales")]
    public async Task<IActionResult> GetWeeklySalesGraph()
    {
        var businessId = GetBusinessId();
        var weeklySales = await _saleService.GetWeeklySalesAsync(businessId);
        return Success(weeklySales, _localizer["Sale.WeeklyGraph.Success"]);
    }

    [HttpGet("monthly-revenue")]
    public async Task<IActionResult> GetMonthlyRevenueGraph([FromQuery] int? year = null)
    {
        var businessId = GetBusinessId();
        var targetYear = year ?? DateTime.UtcNow.Year;
        var monthlyRevenue = await _saleService.GetMonthlyRevenueAsync(businessId, targetYear);
        return Success(monthlyRevenue, _localizer["Sale.MonthlyRevenueGraph.Success", targetYear]);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSale(Guid id, [FromBody] SaleUpdateRequest request)
    {
        var businessId = GetBusinessId();

        try
        {
            var sale = await _saleService.UpdateSaleAsync(businessId, id, request);
            return Success(sale, _localizer["Sale.Update.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Sale.NotFound", id]);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(await TranslateDynamicAsync(ex.Message));
        }
    }
}