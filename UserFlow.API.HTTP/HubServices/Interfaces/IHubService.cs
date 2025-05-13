
using UserFlow.API.Shared.Notifications;


/// *****************************************************************************************
/// @file IHubService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-12
/// @brief Defines the interface for a SignalR ChangeStreams hub client service.
/// @details
/// Supports initializing the connection, managing entity subscriptions,
/// and providing a notification event for incoming change messages.
/// *****************************************************************************************
namespace UserFlow.API.Http.HubServices;

/// <summary>
/// 🔧 Interface for client-side ChangeStreams SignalR hub service.
/// </summary>
public interface IHubService : IAsyncDisposable
{
    #region 🔌 Connection

    /// <summary>
    /// 🔌 Initializes the SignalR hub connection and registers the event handler.
    /// </summary>
    Task InitializeAsync();

    #endregion

    #region 📥 Subscription Management

    /// <summary>
    /// 📥 Subscribes to change notifications for the specified entity.
    /// </summary>
    /// <param name="entityName">The name of the entity (e.g., "Users", "Projects").</param>
    Task SubscribeAsync(string entityName);

    /// <summary>
    /// 📤 Unsubscribes from change notifications for the specified entity.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    Task UnsubscribeAsync(string entityName);

    #endregion

    #region 📢 Notifications

    /// <summary>
    /// 📢 Event triggered when a relevant ChangeNotification is received and matches an active subscription.
    /// </summary>
    event Action<ChangeNotification> OnChangeReceived;

    #endregion
}
