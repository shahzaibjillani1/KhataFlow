using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class BusinessService : IBusinessService
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly IValidator<BusinessAddRequest> _addValidator;
    private readonly IValidator<BusinessUpdateRequest> _updateValidator;
    private readonly IBilingualTextService _bilingual;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BusinessService(
        IBusinessRepository businessRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IMapper mapper,
        IValidator<BusinessAddRequest> addValidator,
        IValidator<BusinessUpdateRequest> updateValidator,
        IBilingualTextService bilingual,
        IStringLocalizer<SharedResource> localizer)
    {
        _businessRepository = businessRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _mapper = mapper;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
        _bilingual = bilingual;
        _localizer = localizer;
    }

    public async Task<List<BusinessResponse>> GetAllBusinessesAsync()
    {
        var businesses = await _businessRepository.GetAllAsync();
        return _mapper.Map<List<BusinessResponse>>(businesses);
    }

    public async Task<BusinessResponse?> GetBusinessByIdAsync(Guid id)
    {
        var business = await _businessRepository.GetByIdAsync(id);
        return business is null ? null : _mapper.Map<BusinessResponse>(business);
    }

    public async Task<BusinessResponse?> GetMyBusinessAsync(Guid userId)
    {
        var business = await _businessRepository.GetByOwnerIdAsync(userId);
        return business is null ? null : _mapper.Map<BusinessResponse>(business);
    }

    public async Task<BusinessResponse> AddBusinessAsync(BusinessAddRequest request, Guid userId)
    {
        var validation = await _addValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var owner = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(_localizer["User.GetById.NotFound", userId]);

        var alreadyExists = await _businessRepository.ExistsByOwnerIdAsync(userId);
        if (alreadyExists)
            throw new ConflictException(_localizer["Business.AlreadyRegistered"]);

        var emailTaken = await _businessRepository.ExistsByEmailAsync(request.OwnerEmail);
        if (emailTaken)
            throw new ConflictException(_localizer["Business.EmailInUse", request.OwnerEmail]);

        var business = _mapper.Map<Business>(request);

        business.Id = userId;
        business.OwnerId = userId;
        business.OwnerEmail = owner.Email!;
        business.Status = BusinessStatus.Active;
        business.SubscriptionPlan = SubscriptionPlanType.Free;
        business.SubscriptionExpiry = DateTime.UtcNow.AddDays(30);

        (business.BusinessName, business.BusinessNameUr) = await _bilingual.ResolveAsync(business.BusinessName);
        (business.OwnerName, business.OwnerNameUr) = await _bilingual.ResolveAsync(owner.FullName);

        if (!string.IsNullOrWhiteSpace(business.Address))
            (business.Address, business.AddressUr) = await _bilingual.ResolveAsync(business.Address);

        var created = await _businessRepository.AddAsync(business);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: "Welcome to KhataFlow!",
            Message: $"Your business '{created.BusinessName}' has been set up on the Free plan.",
            Type: NotificationType.System,
            BusinessId: created.Id));

        return _mapper.Map<BusinessResponse>(created);
    }

    public async Task<BusinessResponse> UpdateBusinessAsync(BusinessUpdateRequest request, Guid userId)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existing = await _businessRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        if (existing.OwnerId != userId)
            throw new ForbiddenException(_localizer["Business.NoPermissionToUpdate"]);

        bool nameChanged = !string.IsNullOrWhiteSpace(request.Name) &&
            (_bilingual.ContainsUrduScript(request.Name)
                ? !string.Equals(existing.BusinessNameUr, request.Name, StringComparison.Ordinal)
                : !string.Equals(existing.BusinessName, request.Name, StringComparison.Ordinal));

        bool addressChanged = !string.IsNullOrWhiteSpace(request.Address) &&
            (_bilingual.ContainsUrduScript(request.Address)
                ? !string.Equals(existing.AddressUr, request.Address, StringComparison.Ordinal)
                : !string.Equals(existing.Address, request.Address, StringComparison.Ordinal));

        bool nameUrStale = _bilingual.IsTranslationStale(existing.BusinessName, existing.BusinessNameUr);
        bool addressUrStale = !string.IsNullOrWhiteSpace(existing.Address)
            && _bilingual.IsTranslationStale(existing.Address, existing.AddressUr);

        _mapper.Map(request, existing);
        existing.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Name) && (nameChanged || nameUrStale))
            (existing.BusinessName, existing.BusinessNameUr) = await _bilingual.ResolveAsync(request.Name);

        if (!string.IsNullOrWhiteSpace(request.Address) && (addressChanged || addressUrStale))
            (existing.Address, existing.AddressUr) = await _bilingual.ResolveAsync(request.Address);

        var updated = await _businessRepository.UpdateAsync(existing);

        return _mapper.Map<BusinessResponse>(updated);
    }

    public async Task<bool> DeleteBusinessAsync(Guid id, Guid userId)
    {
        var business = await _businessRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        if (business.OwnerId != userId)
            throw new ForbiddenException(_localizer["Business.NoPermissionToDelete"]);

        return await _businessRepository.DeleteAsync(id);
    }

    public async Task<bool> SuspendBusinessAsync(Guid businessId, string reason)
    {
        var business = await _businessRepository.GetByIdAsync(businessId)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        var (reasonEn, reasonUr) = await _bilingual.ResolveAsync(reason);
        business.Suspend(reasonEn, reasonUr);
        await _businessRepository.UpdateAsync(business);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: "Account Suspended",
            Message: $"Your business account has been suspended. Reason: {reasonEn}",
            Type: NotificationType.System,
            BusinessId: business.Id));

        return true;
    }

    public async Task<bool> ReactivateBusinessAsync(Guid businessId)
    {
        var business = await _businessRepository.GetByIdAsync(businessId)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        business.Activate();
        await _businessRepository.UpdateAsync(business);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: "Account Reactivated",
            Message: "Your business account has been reactivated. Welcome back!",
            Type: NotificationType.System,
            BusinessId: business.Id));

        return true;
    }

    public async Task<SubscriptionResponse> RenewSubscriptionAsync(Guid businessId)
    {
        var business = await _businessRepository.GetByIdAsync(businessId)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        business.RenewSubscription(30);
        var updated = await _businessRepository.UpdateAsync(business);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: "Subscription Renewed",
            Message: $"Your '{updated.SubscriptionPlan}' plan has been renewed until {updated.SubscriptionExpiry:dd MMM yyyy}.",
            Type: NotificationType.SubscriptionExpiring,
            BusinessId: updated.Id));

        return MapToSubscriptionResponse(updated);
    }

    public async Task<SubscriptionResponse> UpgradePlanAsync(Guid businessId, SubscriptionPlanType newPlan)
    {
        var business = await _businessRepository.GetByIdAsync(businessId)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        if (business.SubscriptionPlan == newPlan)
            throw new DomainException(_localizer["Business.AlreadyOnPlan", newPlan]);

        business.SubscriptionPlan = newPlan;
        business.SubscriptionExpiry = DateTime.UtcNow.AddDays(30);
        business.Status = BusinessStatus.Active;
        business.UpdatedAt = DateTime.UtcNow;

        var updated = await _businessRepository.UpdateAsync(business);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: "Plan Upgraded",
            Message: $"Your business is now on the '{newPlan}' plan.",
            Type: NotificationType.System,
            BusinessId: updated.Id));

        return MapToSubscriptionResponse(updated);
    }

    public async Task<SubscriptionResponse> ChangeSubscriptionAsync(Guid businessId, ChangeSubscriptionRequest request)
    {
        var business = await _businessRepository.GetByIdAsync(businessId)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        if (business.SubscriptionPlan == request.NewPlan)
            throw new DomainException(_localizer["Business.AlreadyOnPlan", request.NewPlan]);

        var isValidToggle =
            (business.SubscriptionPlan == SubscriptionPlanType.Free && request.NewPlan == SubscriptionPlanType.Premium) ||
            (business.SubscriptionPlan == SubscriptionPlanType.Premium && request.NewPlan == SubscriptionPlanType.Free);

        if (!isValidToggle)
            throw new DomainException(_localizer["Business.InvalidPlanChange", business.SubscriptionPlan, request.NewPlan]);

        business.SubscriptionPlan = request.NewPlan;
        business.SubscriptionExpiry = request.CustomExpiryDate ?? DateTime.UtcNow.AddDays(30);
        business.Status = BusinessStatus.Active;
        business.UpdatedAt = DateTime.UtcNow;

        var updated = await _businessRepository.UpdateAsync(business);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: "Subscription Plan Changed",
            Message: $"Your plan has changed to '{request.NewPlan}', effective until {updated.SubscriptionExpiry:dd MMM yyyy}.",
            Type: NotificationType.System,
            BusinessId: updated.Id));

        return MapToSubscriptionResponse(updated);
    }

    public async Task<PlatformSummaryResponse> GetPlatformSummaryAsync()
    {
        var totalUsers = await _businessRepository.GetTotalCountAsync();
        var activeSubscriptions = await _businessRepository.GetActiveSubscriptionsCountAsync();
        var newThisWeek = await _businessRepository.GetNewThisWeekCountAsync();

        return new PlatformSummaryResponse(
            TotalUsers: totalUsers,
            ActiveSubscriptions: activeSubscriptions,
            NewThisWeek: newThisWeek,
            PlatformRevenue: 0m,
            TotalUserSales: 0m,
            ChurnRate: 0m,
            ARPU: 0m
        );
    }

    public async Task<ImpersonationTokenResponse> LoginAsBusinessAsync(Guid businessId)
    {
        var business = await _businessRepository.GetByIdAsync(businessId)
            ?? throw new NotFoundException(_localizer["Business.GetById.NotFound"]);

        return new ImpersonationTokenResponse(
            Token: "NOT_IMPLEMENTED",
            BusinessId: business.Id,
            BusinessName: business.BusinessName,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );
    }

    private async Task TryNotifyAsync(CreateNotificationRequest request)
    {
        try { await _notificationService.CreateNotificationAsync(request); }
        catch { }
    }

    private static SubscriptionResponse MapToSubscriptionResponse(Business b) =>
    new(
        BusinessId: b.Id,
        BusinessName: b.BusinessName,
        BusinessNameUr: b.BusinessNameUr,
        Plan: b.SubscriptionPlan,
        StartDate: b.UpdatedAt ?? b.CreatedAt,
        ExpiryDate: b.SubscriptionExpiry,
        IsActive: b.Status == BusinessStatus.Active && b.SubscriptionExpiry > DateTime.UtcNow
    );

    public async Task<PaginatedResponse<BusinessResponse>> GetBusinessesPagedAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), _localizer["General.PageNumber.Invalid"]);

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), _localizer["General.PageSize.Invalid"]);

        var (items, totalCount) = await _businessRepository.GetPagedAsync(pageNumber, pageSize);
        var mapped = _mapper.Map<List<BusinessResponse>>(items);

        return new PaginatedResponse<BusinessResponse>(mapped, pageNumber, pageSize, totalCount);
    }
}