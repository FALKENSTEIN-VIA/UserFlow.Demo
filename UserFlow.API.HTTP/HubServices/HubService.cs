/// *****************************************************************************************
/// @file HubService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-12
/// @brief Provides a client-side SignalR Hub connection for ChangeStreams notifications.
/// @details
/// Manages SignalR connection, entity subscriptions, and forwards incoming change notifications to subscribed ViewModels via event.
/// *****************************************************************************************

using Microsoft.AspNetCore.SignalR.Client;
using UserFlow.API.Shared.Notifications;

namespace UserFlow.API.Http.HubServices;

/// <summary>
/// 🔔 Service for managing SignalR ChangeStreams client connection and subscriptions.
/// </summary>
public class HubService : IHubService
{
    #region 🔐 Dependencies & Fields

    private readonly string _hubUrl;
    private HubConnection _hubConnection = null!;
    private readonly HashSet<string> _subscriptions = new();

    #endregion

    #region 📢 Events

    /// <summary>
    /// 📢 Event fired when a relevant ChangeNotification is received and matches an active subscription.
    /// </summary>
    public event Action<ChangeNotification>? OnChangeReceived;

    #endregion

    #region 🔧 Constructor

    public HubService(string hubUrl)
    {
        _hubUrl = hubUrl;
    }

    #endregion

    #region 🔌 Initialization

    /// <summary>
    /// 🔌 Initializes the SignalR connection and registers the global ReceiveChange handler.
    /// </summary>
    public async Task InitializeAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ChangeNotification>("ReceiveChange", notification =>
        {
            Console.WriteLine($"📥 Received change: {notification.EntityName} ({notification.Operation}) {notification.EntityId}");

            if (!_subscriptions.Contains(notification.EntityName)) return;

            // 📨 Raise event to ViewModels
            OnChangeReceived?.Invoke(notification);
        });

        await _hubConnection.StartAsync();
        Console.WriteLine($"✅ Hub connected to {_hubUrl}");
    }

    #endregion

    #region 📥 Subscription Management

    public async Task SubscribeAsync(string entityName)
    {
        if (_hubConnection?.State != HubConnectionState.Connected) return;
        if (_subscriptions.Contains(entityName)) return;

        await _hubConnection.InvokeAsync("SubscribeToEntity", entityName);
        _subscriptions.Add(entityName);
    }

    public async Task UnsubscribeAsync(string entityName)
    {
        if (_hubConnection?.State != HubConnectionState.Connected) return;
        if (!_subscriptions.Contains(entityName)) return;

        await _hubConnection.InvokeAsync("UnsubscribeFromEntity", entityName);
        _subscriptions.Remove(entityName);
    }

    #endregion

    #region 🗑️ Cleanup

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
    }

    #endregion
}
