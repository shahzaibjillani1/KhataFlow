using KhataFlow.Core.DTO;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

public class BusinessController : CustomControllerBase
{
    private readonly IBusinessService _businessService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public BusinessController(
        IBusinessService businessService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _businessService = businessService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    
    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet]
    public async Task<IActionResult> GetAllBusinesses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _businessService.GetBusinessesPagedAsync(pageNumber, pageSize);
        return Success(result, _localizer["Business.GetAll.Success"]);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var data = await _businessService.GetBusinessByIdAsync(id);
        if (data == null)
            return NotFoundResponse(_localizer["Business.GetById.NotFound"]);

        return Success(data, _localizer["Business.GetById.Success"]);
    }

    [HttpGet("platform-summary")]
    public async Task<IActionResult> GetPlatformSummary()
    {
        var data = await _businessService.GetPlatformSummaryAsync();
        return Success(data, _localizer["Business.PlatformSummary.Success"]);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BusinessAddRequest request)
    {
        var userId = GetCurrentUserId();

        try
        {
            var data = await _businessService.AddBusinessAsync(request, userId);
            return Created(data, _localizer["Business.Create.Success"]);
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
    public async Task<IActionResult> Update(Guid id, [FromBody] BusinessUpdateRequest request)
    {
        if (id != request.Id)
            return BadRequestResponse(_localizer["Business.Update.IdMismatch"]);

        var userId = GetCurrentUserId();

        try
        {
            var data = await _businessService.UpdateBusinessAsync(request, userId);
            return Success(data, _localizer["Business.Update.Success"]);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(await TranslateDynamicAsync(ex.Message));
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

    [HttpPatch("{id:guid}/suspend")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Suspend(Guid id, [FromQuery] string reason)
    {
        try
        {
            var result = await _businessService.SuspendBusinessAsync(id, reason);

            if (!result)
                return BadRequestResponse(_localizer["Business.Suspend.Failed"]);

            return Success<object>(null, _localizer["Business.Suspend.Success"]);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpPatch("{id:guid}/reactivate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        try
        {
            var result = await _businessService.ReactivateBusinessAsync(id);

            if (!result)
                return BadRequestResponse(_localizer["Business.Reactivate.Failed"]);

            return Success<object>(null, _localizer["Business.Reactivate.Success"]);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpPost("{id:guid}/renew-subscription")]
    public async Task<IActionResult> RenewSubscription(Guid id)
    {
        try
        {
            var data = await _businessService.RenewSubscriptionAsync(id);
            return Success(data, _localizer["Business.RenewSubscription.Success"]);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpPost("{id:guid}/change-subscription")]
    public async Task<IActionResult> ChangeSubscription(Guid id, [FromBody] ChangeSubscriptionRequest request)
    {
        try
        {
            var data = await _businessService.ChangeSubscriptionAsync(id, request);
            return Success(data, _localizer["Business.ChangeSubscription.Success"]);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(await TranslateDynamicAsync(ex.Message));
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

    [HttpPost("{id:guid}/impersonation-token")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> LoginAs(Guid id)
    {
        try
        {
            var data = await _businessService.LoginAsBusinessAsync(id);
            return Success(data, _localizer["Business.Impersonation.Success"]);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
    }
}