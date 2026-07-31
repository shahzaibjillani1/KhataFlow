using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Mappers;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly ITokenService _tokenService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPlanLimitService _planLimitService;
    private readonly IAIClientService _aiClient;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UserService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IBusinessRepository businessRepository,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager,
        IPlanLimitService planLimitService,
        IAIClientService aiClient,
        IStringLocalizer<SharedResource> localizer
    )
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _businessRepository = businessRepository;
        _notificationService = notificationService;
        _planLimitService = planLimitService;
        _userManager = userManager;
        _aiClient = aiClient;
        _localizer = localizer;
    }

    public async Task<List<UserResponse>> GetUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(u => u.ToUserResponse()).ToList();
    }

    public async Task<AuthResponse?> Login(LoginRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email, nameof(request.Email));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password, nameof(request.Password));

        var identifier = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(identifier.ToLowerInvariant())
            ?? await _userManager.FindByNameAsync(identifier);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException(_localizer["Auth.Login.InvalidCredentials"]);

        return await _tokenService.GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse?> Register(RegisterRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email, nameof(request.Email));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password, nameof(request.Password));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FullName, nameof(request.FullName));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingEmail = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingEmail is not null)
            throw new InvalidOperationException(_localizer["User.EmailAlreadyExists"]);

        var businessId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Owner,
            BusinessId = businessId,
        };

        var fullNameUrTask = _aiClient.TranslateAsync(user.FullName, "ur");
        var businessNameUrTask = _aiClient.TranslateAsync(request.BusinessName, "ur");
        await Task.WhenAll(fullNameUrTask, businessNameUrTask);

        user.FullNameUr = fullNameUrTask.Result;

        await _userRepository.AddAsync(user, request.Password);

        var business = new Business
        {
            Id = businessId,
            BusinessName = request.BusinessName,
            BusinessNameUr = businessNameUrTask.Result,
            OwnerId = user.Id,
            SubscriptionExpiry = DateTime.UtcNow.AddDays(30),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
        };

        await _businessRepository.AddAsync(business);

        await TryNotifyAsync(
            new CreateNotificationRequest(
                Target: NotificationTarget.Business,
                Title: _localizer["User.Notification.Welcome.Title"],
                Message: string.Format(
                    _localizer["User.Notification.Welcome.Message"],
                    user.FullName,
                    business.BusinessName,
                    business.SubscriptionExpiry
                ),
                Type: NotificationType.WelcomeMessage,
                BusinessId: businessId,
                ReferenceId: user.Id
            )
        );

        await TryNotifyAsync(
            new CreateNotificationRequest(
                Target: NotificationTarget.Admin,
                Title: _localizer["User.Notification.NewBusinessRegistered.Title"],
                Message: string.Format(
                    _localizer["User.Notification.NewBusinessRegistered.Message"],
                    business.BusinessName,
                    user.Email
                ),
                Type: NotificationType.NewBusinessRegistered,
                BusinessId: Guid.Empty,
                ReferenceId: businessId
            )
        );

        return await _tokenService.GenerateAuthResponseAsync(user);
    }

    private async Task TryNotifyAsync(CreateNotificationRequest request)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(request);
        }
        catch { }
    }

    public async Task<UserResponse> EditUserAsync(
        Guid targetUserId,
        Guid requestingUserId,
        UserUpdateRequest request
    )
    {
        var user =
            await _userManager.FindByIdAsync(targetUserId.ToString())
            ?? throw new NotFoundException(_localizer["User.GetById.NotFound", targetUserId]);

        if (targetUserId != requestingUserId)
            throw new ForbiddenException(_localizer["User.NoPermissionToEdit"]);

        var nameChanged =
            !string.IsNullOrWhiteSpace(request.FullName)
            && !string.Equals(request.FullName, user.FullName, StringComparison.Ordinal);
        var displayNameChanged =
            !string.IsNullOrWhiteSpace(request.DisplayName)
            && !string.Equals(request.DisplayName, user.DisplayName, StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName;

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;

        if (request.Gender.HasValue)
            user.Gender = request.Gender.Value;

        if (request.DateOfBirth.HasValue)
            user.DateOfBirth = request.DateOfBirth.Value;

        if (nameChanged)
            user.FullNameUr = await _aiClient.TranslateAsync(user.FullName!, "ur");

        if (displayNameChanged)
            user.DisplayNameUr = await _aiClient.TranslateAsync(user.DisplayName!, "ur");

        string? newNormalizedEmail = null;

        if (
            !string.IsNullOrWhiteSpace(request.Email)
            && !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase)
        )
        {
            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing is not null && existing.Id != user.Id)
                throw new ConflictException(_localizer["User.EmailInUse", request.Email]);

            newNormalizedEmail = request.Email.ToLowerInvariant();

            var emailResult = await _userManager.SetEmailAsync(user, newNormalizedEmail);
            if (!emailResult.Succeeded)
                throw new DomainException(
                    string.Join("; ", emailResult.Errors.Select(e => e.Description))
                );

            var usernameResult = await _userManager.SetUserNameAsync(user, newNormalizedEmail);
            if (!usernameResult.Succeeded)
                throw new DomainException(
                    string.Join("; ", usernameResult.Errors.Select(e => e.Description))
                );
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new DomainException(
                string.Join("; ", updateResult.Errors.Select(e => e.Description))
            );

        if (newNormalizedEmail is not null && user.Role == UserRole.Owner)
        {
            await TrySyncBusinessOwnerEmailAsync(user.Id, newNormalizedEmail);
        }

        return user.ToUserResponse();
    }

    private async Task TrySyncBusinessOwnerEmailAsync(Guid ownerId, string newEmail)
    {
        try
        {
            var business = await _businessRepository.GetByOwnerIdAsync(ownerId);
            if (business is null)
                return;

            business.OwnerEmail = newEmail;
            business.UpdatedAt = DateTime.UtcNow;

            await _businessRepository.UpdateAsync(business);
        }
        catch { }
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        var user =
            await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException(_localizer["User.GetById.NotFound", id]);

        return user.ToUserResponse();
    }

    public async Task<List<UserResponse>> GetBusinessUsersAsync(Guid businessId)
    {
        var users = await _userRepository.GetByBusinessIdAsync(businessId);
        return users.Select(u => u.ToUserResponse()).ToList();
    }

    public async Task<StaffInviteResponse> InviteStaffAsync(
        Guid requestingUserId,
        InviteStaffRequest request
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FullName, nameof(request.FullName));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PhoneNumber, nameof(request.PhoneNumber));

        var requestingUser =
            await _userManager.FindByIdAsync(requestingUserId.ToString())
            ?? throw new NotFoundException(_localizer["User.GetById.NotFound", requestingUserId]);

        if (requestingUser.Role != UserRole.Owner)
            throw new ForbiddenException(_localizer["User.NoPermissionToInvite"]);

        if (request.Role is not (UserRole.Manager or UserRole.Staff))
            throw new DomainException(_localizer["User.Invite.InvalidRole"]);

        await _planLimitService.EnsureCanAddStaffAsync(requestingUser.BusinessId);

        var normalizedPhone = NormalizePhoneForWhatsApp(request.PhoneNumber);
        var placeholderEmail = string.IsNullOrWhiteSpace(request.Email)
            ? $"{Guid.NewGuid():N}@khataflow.local"
            : request.Email.Trim().ToLowerInvariant();

        var existing = await _userManager.FindByEmailAsync(placeholderEmail);
        if (existing is not null)
            throw new ConflictException(_localizer["User.EmailAlreadyExists"]);

        var tempPassword = GenerateTempPassword();

        var staffUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = placeholderEmail,
            UserName = request.PhoneNumber.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = request.Role,
            BusinessId = requestingUser.BusinessId,
            Status = AccountStatus.PendingVerification,
        };

        staffUser.FullNameUr = await _aiClient.TranslateAsync(staffUser.FullName, "ur");

        await _userRepository.AddAsync(staffUser, tempPassword);

        var business = await _businessRepository.GetByIdAsync(requestingUser.BusinessId);

        var message = string.Format(
            _localizer["User.Invite.WhatsAppMessage"],
            staffUser.FullName,
            business?.BusinessName,
            staffUser.PhoneNumber,
            tempPassword
        );

        var whatsAppUrl = $"https://wa.me/{normalizedPhone}?text={Uri.EscapeDataString(message)}";

        await TryNotifyAsync(
            new CreateNotificationRequest(
                Target: NotificationTarget.Business,
                Title: _localizer["User.Notification.StaffInvited.Title"],
                Message: string.Format(
                    _localizer["User.Notification.StaffInvited.Message"],
                    staffUser.FullName,
                    request.Role
                ),
                Type: NotificationType.StaffInvited,
                BusinessId: requestingUser.BusinessId,
                ReferenceId: staffUser.Id
            )
        );

        return new StaffInviteResponse(staffUser.ToUserResponse(), whatsAppUrl);
    }

    private static string NormalizePhoneForWhatsApp(string phone)
    {
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        if (digitsOnly.StartsWith("0"))
            digitsOnly = "92" + digitsOnly[1..];

        return digitsOnly;
    }

    private static string GenerateTempPassword() => $"Kf{Random.Shared.Next(100000, 999999)}!";
}
