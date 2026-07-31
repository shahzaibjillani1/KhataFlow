using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class CustomControllerBase : ControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    protected CustomControllerBase(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    protected Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(raw, out var id) || id == Guid.Empty)
            throw new UnauthorizedException(_localizer["Auth.UserIdentity.Undetermined"]);

        return id;
    }

    protected Guid? TryGetBusinessId()
    {
        var raw = User.FindFirstValue("businessId");
        if (!Guid.TryParse(raw, out var id) || id == Guid.Empty)
            return null;
        return id;
    }

    protected Guid GetBusinessId()
    {
        return TryGetBusinessId() ?? throw new BusinessNotFoundException(
            _localizer["Business.NotFoundForAccount"]);
    }

    protected IActionResult Success<T>(T data, string message) =>
        Ok(new ApiResponse<T> { Result = true, Message = message, Data = data });

    protected IActionResult Created<T>(T data, string message) =>
        StatusCode(StatusCodes.Status201Created,
            new ApiResponse<T> { Result = true, Message = message, Data = data });

    protected IActionResult NoContentResponse(string message) =>
        Ok(new ApiResponse<object> { Result = true, Message = message, Data = null });

    protected IActionResult BadRequestResponse(string message) =>
        BadRequest(new ApiResponse<object> { Result = false, Message = message, Data = null });

    protected IActionResult NotFoundResponse(string message) =>
        NotFound(new ApiResponse<object> { Result = false, Message = message, Data = null });

    protected IActionResult UnauthorizedResponse(string message) =>
        Unauthorized(new ApiResponse<object> { Result = false, Message = message, Data = null });

    protected IActionResult ConflictResponse(string message) =>
        Conflict(new ApiResponse<object> { Result = false, Message = message, Data = null });

    protected IActionResult ValidationFailure(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return BadRequest(new ApiValidationResponse(
            Success: false,
            Message: _localizer["General.ValidationFailed"],
            Errors: errors
        ));
    }
}