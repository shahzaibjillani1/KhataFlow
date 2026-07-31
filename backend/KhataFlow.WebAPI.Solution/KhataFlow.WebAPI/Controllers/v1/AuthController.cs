using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class AuthController : CustomControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public AuthController(
        ITokenService tokenService,
        IUserService userService,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _tokenService = tokenService;
        _userService = userService;
        _emailService = emailService;
        _userManager = userManager;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationFailure(ModelState);

        try
        {
            var result = await _userService.Register(request);
            return Created(result, _localizer["Auth.Register.Success"]);
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

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationFailure(ModelState);

        try
        {
            var result = await _userService.Login(request);
            return Success(result, _localizer["Auth.Login.Success"]);
        }
        catch (UnauthorizedAccessException)
        {
            return UnauthorizedResponse(_localizer["Auth.Login.InvalidCredentials"]);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationFailure(ModelState);

        try
        {
            var result = await _tokenService.RefreshTokenAsync(request);
            return Success(result, _localizer["Auth.Refresh.Success"]);
        }
        catch (SecurityTokenException ex)
        {
            return UnauthorizedResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
            return UnauthorizedResponse(_localizer["Auth.Logout.UserIdentityUnresolved"]);

        await _tokenService.RevokeRefreshTokenAsync(userId);
        return NoContentResponse(_localizer["Auth.Logout.Success"]);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationFailure(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Auth",
                new { token, email = user.Email }, Request.Scheme);

            await _emailService.SendEmailAsync(user.Email,
                _localizer["Auth.Email.ResetSubject"],
                $"""
            <h3>{_localizer["Auth.Email.ResetHeading"]}</h3>
            <p>{_localizer["Auth.Email.ResetInstruction"]}</p>
            <a href='{resetLink}'>{_localizer["Auth.Email.ResetLinkText"]}</a>
            <p>{_localizer["Auth.Email.ResetExpiry"]}</p>
            """);
        }

        return Success<object>(null, _localizer["Auth.ForgotPassword.LinkSent"]);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationFailure(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequestResponse(_localizer["Auth.ResetPassword.InvalidRequest"]);

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequestResponse(await TranslateDynamicAsync(errors));
        }

        return Success<object>(null, _localizer["Auth.ResetPassword.Success"]);
    }
}