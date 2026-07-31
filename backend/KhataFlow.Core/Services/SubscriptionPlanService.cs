using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IBilingualTextService _bilingual;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository repository,
        INotificationService notificationService,
        IBilingualTextService bilingual,
        IStringLocalizer<SharedResource> localizer)
    {
        _repository = repository;
        _notificationService = notificationService;
        _bilingual = bilingual;
        _localizer = localizer;
    }

    private async Task<SubscriptionPlanResponse> MapToResponseAsync(SubscriptionPlan plan)
    {
        var userCount = await _repository.GetUserCountByPlanAsync(plan.Id);
        var totalRevenue = await _repository.GetRevenueByPlanAsync(plan.Id);

        return new SubscriptionPlanResponse(
            plan.Id,
            plan.PlanName,
            plan.PlanNameUr,
            plan.MonthlyPrice,
            plan.Features.ToList(),
            plan.FeaturesUr.ToList(),
            plan.PlanType,
            plan.IsActive,
            userCount,
            totalRevenue
        );
    }

    public async Task<SubscriptionPlanResponse> AddPlanAsync(SubscriptionPlanAddRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.PlanName))
            throw new ArgumentException(_localizer["SubscriptionPlan.PlanName.Empty"], nameof(request));

        if (request.MonthlyPrice < 0)
            throw new ArgumentException(_localizer["SubscriptionPlan.MonthlyPrice.Negative"], nameof(request));

        bool exists = await _repository.ExistsAsync(request.PlanName);
        if (exists)
            throw new InvalidOperationException(_localizer["SubscriptionPlan.AlreadyExists", request.PlanName]);

        var planNameInput = request.PlanName.Trim();
        var featuresInput = request.Features ?? [];

        var (planName, planNameUr) = await _bilingual.ResolveAsync(planNameInput);
        var (features, featuresUr) = await ResolveFeaturesAsync(featuresInput);

        var plan = new SubscriptionPlan
        {
            PlanName = planName,
            PlanNameUr = planNameUr,
            MonthlyPrice = request.MonthlyPrice,
            Features = features,
            FeaturesUr = featuresUr,
            PlanType = request.PlanType,
            IsActive = true
        };

        var created = await _repository.AddAsync(plan);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Admin,
            Title: _localizer["SubscriptionPlan.Notification.Created.Title"],
            Message: string.Format(_localizer["SubscriptionPlan.Notification.Created.Message"], created.PlanName, created.MonthlyPrice),
            Type: NotificationType.PlanCreated,
            BusinessId: Guid.Empty,
            ReferenceId: created.Id));

        return await MapToResponseAsync(created);
    }

    public async Task<SubscriptionPlanResponse> UpdatePlanAsync(SubscriptionPlanUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.PlanName))
            throw new ArgumentException(_localizer["SubscriptionPlan.PlanName.Empty"], nameof(request));

        if (request.MonthlyPrice < 0)
            throw new ArgumentException(_localizer["SubscriptionPlan.MonthlyPrice.Negative"], nameof(request));

        var existing = await _repository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException(_localizer["SubscriptionPlan.NotFoundById", request.Id]);

        var planNameInput = request.PlanName.Trim();
        var featuresInput = request.Features ?? [];

        bool nameChanged = _bilingual.ContainsUrduScript(planNameInput)
            ? !string.Equals(existing.PlanNameUr, planNameInput, StringComparison.Ordinal)
            : !string.Equals(existing.PlanName, planNameInput, StringComparison.Ordinal);

        bool nameUrStale = _bilingual.IsTranslationStale(existing.PlanName, existing.PlanNameUr);

        bool featuresChanged = !existing.Features.SequenceEqual(featuresInput);
        bool featuresUrStale = existing.Features.Count > 0 &&
            (existing.FeaturesUr.Count != existing.Features.Count
                || existing.Features.Zip(existing.FeaturesUr, (en, ur) => _bilingual.IsTranslationStale(en, ur)).Any(stale => stale));

        string planName = existing.PlanName;
        string planNameUr = existing.PlanNameUr;
        if (nameChanged || nameUrStale)
            (planName, planNameUr) = await _bilingual.ResolveAsync(planNameInput);

        ICollection<string> features = existing.Features;
        ICollection<string> featuresUr = existing.FeaturesUr;
        if (featuresChanged || featuresUrStale)
            (features, featuresUr) = await ResolveFeaturesAsync(featuresInput);

        var updated = new SubscriptionPlan
        {
            Id = request.Id,
            PlanName = planName,
            PlanNameUr = planNameUr,
            MonthlyPrice = request.MonthlyPrice,
            Features = features,
            FeaturesUr = featuresUr,
            PlanType = existing.PlanType,
            IsActive = request.IsActive
        };

        var saved = await _repository.UpdateAsync(updated);

        if (existing.MonthlyPrice != saved.MonthlyPrice)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Admin,
                Title: _localizer["SubscriptionPlan.Notification.PriceChanged.Title"],
                Message: string.Format(_localizer["SubscriptionPlan.Notification.PriceChanged.Message"], saved.PlanName, existing.MonthlyPrice, saved.MonthlyPrice),
                Type: NotificationType.PlanPriceChanged,
                BusinessId: Guid.Empty,
                ReferenceId: saved.Id));
        }

        if (existing.IsActive && !saved.IsActive)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Admin,
                Title: _localizer["SubscriptionPlan.Notification.Deactivated.Title"],
                Message: string.Format(_localizer["SubscriptionPlan.Notification.Deactivated.Message"], saved.PlanName),
                Type: NotificationType.PlanDeactivated,
                BusinessId: Guid.Empty,
                ReferenceId: saved.Id));
        }
        else if (!existing.IsActive && saved.IsActive)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Admin,
                Title: _localizer["SubscriptionPlan.Notification.Reactivated.Title"],
                Message: string.Format(_localizer["SubscriptionPlan.Notification.Reactivated.Message"], saved.PlanName),
                Type: NotificationType.PlanReactivated,
                BusinessId: Guid.Empty,
                ReferenceId: saved.Id));
        }

        return await MapToResponseAsync(saved);
    }

    public async Task<bool> DeletePlanAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(_localizer["SubscriptionPlan.Id.Empty"], nameof(id));

        var existing = await _repository.GetByIdAsync(id);

        var result = await _repository.DeleteAsync(id);

        if (result && existing is not null)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Admin,
                Title: _localizer["SubscriptionPlan.Notification.Deleted.Title"],
                Message: string.Format(_localizer["SubscriptionPlan.Notification.Deleted.Message"], existing.PlanName),
                Type: NotificationType.PlanDeleted,
                BusinessId: Guid.Empty,
                ReferenceId: id));
        }

        return result;
    }

    public async Task<List<SubscriptionPlanResponse>> GetAllPlansAsync()
    {
        var plans = await _repository.GetAllAsync();

        var results = new List<SubscriptionPlanResponse>();
        foreach (var plan in plans)
        {
            results.Add(await MapToResponseAsync(plan));
        }

        return results;
    }

    public async Task<SubscriptionPlanResponse?> GetPlanByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(_localizer["SubscriptionPlan.Id.Empty"], nameof(id));

        var plan = await _repository.GetByIdAsync(id);
        return plan is null ? null : await MapToResponseAsync(plan);
    }

    public async Task<int> GetUserCountByPlanAsync(Guid planId)
    {
        if (planId == Guid.Empty)
            throw new ArgumentException(_localizer["SubscriptionPlan.PlanId.Empty"], nameof(planId));

        _ = await _repository.GetByIdAsync(planId)
            ?? throw new KeyNotFoundException(_localizer["SubscriptionPlan.NotFoundById", planId]);

        return await _repository.GetUserCountByPlanAsync(planId);
    }

    public async Task<decimal> GetRevenueByPlanAsync(Guid planId)
    {
        if (planId == Guid.Empty)
            throw new ArgumentException(_localizer["SubscriptionPlan.PlanId.Empty"], nameof(planId));

        return await _repository.GetRevenueByPlanAsync(planId);
    }

    private async Task<(ICollection<string> English, ICollection<string> Urdu)> ResolveFeaturesAsync(ICollection<string> features)
    {
        var english = new List<string>(features.Count);
        var urdu = new List<string>(features.Count);

        foreach (var feature in features)
        {
            var (en, ur) = await _bilingual.ResolveAsync(feature);
            english.Add(en);
            urdu.Add(ur);
        }

        return (english, urdu);
    }

    private async Task TryNotifyAsync(CreateNotificationRequest request)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(request);
        }
        catch
        {
        }
    }
}