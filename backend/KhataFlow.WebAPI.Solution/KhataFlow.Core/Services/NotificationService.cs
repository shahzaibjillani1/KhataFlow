using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace KhataFlow.Core.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationSenderService _notificationSender;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;
    private readonly IAIClientService _aiClient;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationSenderService notificationSender,
        IMapper mapper,
        ILogger<NotificationService> logger,
        IAIClientService aiClient,
        IStringLocalizer<SharedResource> localizer)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiClient = aiClient ?? throw new ArgumentNullException(nameof(aiClient));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<NotificationResponse> CreateNotificationAsync(
    CreateNotificationRequest request,
    CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Target == NotificationTarget.User && request.UserId is null)
            throw new ArgumentException(_localizer["Notification.UserIdRequired"], nameof(request));

        if (request.Target == NotificationTarget.Business && request.BusinessId is null)
            throw new ArgumentException(_localizer["Notification.BusinessIdRequired"], nameof(request));

        var notification = new Notification
        {
            Target = request.Target,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            UserId = request.UserId,
            BusinessId = request.BusinessId,
            ReferenceId = request.ReferenceId
        };

        try
        {
            var titleTask = _aiClient.TranslateAsync(notification.Title, "ur");
            var messageTask = _aiClient.TranslateAsync(notification.Message, "ur");
            await Task.WhenAll(titleTask, messageTask);

            notification.TitleUr = titleTask.Result;
            notification.MessageUr = messageTask.Result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Urdu translation failed for notification, falling back to English text");
            notification.TitleUr = notification.Title;
            notification.MessageUr = notification.Message;
        }

        Notification created = await _notificationRepository.AddNotificationAsync(notification, ct);
        NotificationResponse response = _mapper.Map<NotificationResponse>(created);

        try
        {
            Task signalRTask = request.Target switch
            {
                NotificationTarget.User when created.UserId.HasValue =>
                    _notificationSender.SendToUserAsync(created.UserId.Value, response, ct),

                NotificationTarget.Business when created.BusinessId.HasValue =>
                    _notificationSender.SendToBusinessAsync(created.BusinessId.Value, response, ct),

                NotificationTarget.Admin =>
                    _notificationSender.SendToRoleAsync("SuperAdmin", response, ct),

                NotificationTarget.All =>
                    _notificationSender.SendToAllAsync(response, ct),

                _ => Task.CompletedTask
            };

            await signalRTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR delivery failed for notification {NotificationId}", created.Id);
        }

        return response;
    }

    public async Task<NotificationResponse> GetNotificationByIdAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId, ct)
            ?? throw new KeyNotFoundException(_localizer["Notification.NotFound", notificationId]);

        return _mapper.Map<NotificationResponse>(notification);
    }

    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        var notifications = await _notificationRepository.GetNotificationsForUserAsync(userId, businessId, ct);
        return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
    }

    public async Task<IEnumerable<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        var notifications = await _notificationRepository.GetUnreadForUserAsync(userId, businessId, ct);
        return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId, businessId, ct);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        await _notificationRepository.MarkAsReadAsync(notificationId, ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, Guid businessId, CancellationToken ct = default)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId, businessId, ct);
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, CancellationToken ct = default)
    {
        return await _notificationRepository.DeleteNotificationAsync(notificationId, ct);
    }
}