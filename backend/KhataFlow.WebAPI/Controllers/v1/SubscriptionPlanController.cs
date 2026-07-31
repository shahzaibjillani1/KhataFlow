using Asp.Versioning;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class SubscriptionPlanController : CustomControllerBase
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public SubscriptionPlanController(
        ISubscriptionPlanService subscriptionPlanService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _subscriptionPlanService = subscriptionPlanService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _subscriptionPlanService.GetAllPlansAsync();
        return Success(data, _localizer["SubscriptionPlan.GetAll.Success"]);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var data = await _subscriptionPlanService.GetPlanByIdAsync(id);

        if (data == null)
            return NotFoundResponse(_localizer["SubscriptionPlan.NotFound"]);

        return Success(data, _localizer["SubscriptionPlan.GetById.Success"]);
    }

    [HttpGet("{id:guid}/user-count")]
    public async Task<IActionResult> GetUserCount(Guid id)
    {
        var data = await _subscriptionPlanService.GetUserCountByPlanAsync(id);
        return Success(data, _localizer["SubscriptionPlan.UserCount.Success"]);
    }

    [HttpGet("{id:guid}/revenue")]
    public async Task<IActionResult> GetRevenue(Guid id)
    {
        var data = await _subscriptionPlanService.GetRevenueByPlanAsync(id);
        return Success(data, _localizer["SubscriptionPlan.Revenue.Success"]);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubscriptionPlanAddRequest request)
    {
        try
        {
            var data = await _subscriptionPlanService.AddPlanAsync(request);
            return Created(data, _localizer["SubscriptionPlan.Create.Success"]);
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SubscriptionPlanUpdateRequest request)
    {
        if (id != request.Id)
            return BadRequestResponse(_localizer["SubscriptionPlan.Update.IdMismatch"]);

        try
        {
            var data = await _subscriptionPlanService.UpdatePlanAsync(request);
            return Success(data, _localizer["SubscriptionPlan.Update.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["SubscriptionPlan.NotFound"]);
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
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _subscriptionPlanService.DeletePlanAsync(id);

        if (!result)
            return NotFoundResponse(_localizer["SubscriptionPlan.NotFound"]);

        return Success<object>(null, _localizer["SubscriptionPlan.Delete.Success"]);
    }
}