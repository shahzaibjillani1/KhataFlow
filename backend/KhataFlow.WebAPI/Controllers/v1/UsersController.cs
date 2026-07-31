using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class UsersController : CustomControllerBase
{
    private readonly IUserService _userService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public UsersController(
        IUserService userService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _userService = userService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var requestingUserId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(requestingUserId);
        return Success(user, _localizer["User.GetById.Success"]);
    }

    [Authorize(Policy = "OwnerOnly")]
    [HttpGet("business")]
    public async Task<IActionResult> GetBusinessUsers()
    {
        var requestingUserId = GetCurrentUserId();
        var requestingUser = await _userService.GetUserByIdAsync(requestingUserId);
        var users = await _userService.GetBusinessUsersAsync(requestingUser.BusinessId);
        return Success(users, _localizer["User.GetAll.Success"]);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditUser(Guid id, [FromBody] UserUpdateRequest request)
    {
        var requestingUserId = GetCurrentUserId();

        try
        {
            var data = await _userService.EditUserAsync(id, requestingUserId, request);
            return Success(data, _localizer["User.Update.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["User.GetById.NotFound", id]);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResponse(await TranslateDynamicAsync(ex.Message));
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
            return NotFoundResponse(_localizer["User.GetById.NotFound", id]);

        return Success(user, _localizer["User.GetById.Success"]);
    }

    [Authorize(Policy = "OwnerOnly")]
    [HttpPost("staff/invite")]
    public async Task<IActionResult> InviteStaff([FromBody] InviteStaffRequest request)
    {
        var requestingUserId = GetCurrentUserId();
        var result = await _userService.InviteStaffAsync(requestingUserId, request);
        return Success(result, _localizer["User.InviteStaff.Success"]);
    }
}