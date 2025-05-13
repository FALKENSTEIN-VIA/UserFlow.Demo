using Microsoft.Extensions.Logging;
using UserFlow.API.Http.HubServices;
using UserFlow.API.HTTP.Base;
using UserFlow.API.Shared.Notifications;

namespace UserFlow.API.Http.Base;

public abstract partial class BaseChangeStreamsViewModel : BaseViewModel
{
    private readonly IHubService _hubService;

    protected BaseChangeStreamsViewModel(IHubService hubService, ILogger logger) : base(logger)
    {
        _hubService = hubService;
    }

    protected abstract string ChangeStreamEntityName { get; }

    public override async Task OnViewAppearingAsync()
    {
        _hubService.OnChangeReceived += OnChangeReceived;
        await _hubService.SubscribeAsync(ChangeStreamEntityName);
        _logger.LogInformation("🔔 Subscribed to ChangeStreams for {Entity}.", ChangeStreamEntityName);
    }

    public override async Task OnViewDisappearingAsync()
    {
        await _hubService.UnsubscribeAsync(ChangeStreamEntityName);
        _hubService.OnChangeReceived -= OnChangeReceived;
        _logger.LogInformation("🚪 Unsubscribed from ChangeStreams for {Entity}.", ChangeStreamEntityName);
    }

    private void OnChangeReceived(ChangeNotification notification)
    {
        if (notification.EntityName != ChangeStreamEntityName) return;
        _logger.LogInformation("📥 ChangeNotification received: {Entity} {Id} {Op}", notification.EntityName, notification.EntityId, notification.Operation);
        OnChangeNotificationReceived(notification);
    }

    protected abstract void OnChangeNotificationReceived(ChangeNotification notification);
}
