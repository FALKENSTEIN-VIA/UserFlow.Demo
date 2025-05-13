/// *****************************************************************************************
/// @file EntityCollectionExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-13
/// @brief Extension methods to simplify ChangeStreams collection handling in ViewModels.
/// *****************************************************************************************

using System.Collections.ObjectModel;
using UserFlow.API.ChangeStreams.Helper;
using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.Notifications;

namespace UserFlow.API.ChangeStreams;

public static class EntityCollectionExtensions
{
    public static void ApplyEntityChange<T>(
         this ObservableCollection<T> collection,
         ChangeNotification notification,
         Func<long, Task<T?>> loadItemByIdAsync,
         Action<Action> runOnUiThread)
         where T : IEntityDTO<long>
    {
        CollectionChangeHandler.ApplyChange(
            collection,
            notification,
            loadItemByIdAsync,
            entity => entity.Id,
            runOnUiThread);
    }
}
