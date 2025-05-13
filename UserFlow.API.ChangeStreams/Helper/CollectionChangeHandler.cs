/// *****************************************************************************************
/// @file CollectionChangeHandler.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-13
/// @brief Provides smart collection change handling for SignalR ChangeStreams notifications.
/// *****************************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserFlow.API.Shared.Notifications;

namespace UserFlow.API.ChangeStreams.Helper;

public static class CollectionChangeHandler
{
    public static void ApplyChange<T, TId>(
    ObservableCollection<T> collection,
    ChangeNotification notification,
    Func<TId, Task<T?>> loadItemByIdAsync,
    Func<T, TId> getId,
    Action<Action> runOnUiThread)
    {
        _ = Task.Run(async () =>
        {
            var id = (TId)Convert.ChangeType(notification.EntityId, typeof(TId))!;
            var existingItem = collection.FirstOrDefault(x => getId(x)!.Equals(id));

            switch (notification.Operation)
            {
                case "INSERT":
                    var newItem = await loadItemByIdAsync(id);
                    if (newItem != null && existingItem == null)
                        runOnUiThread(() => collection.Add(newItem));
                    break;

                case "UPDATE":
                    var updatedItem = await loadItemByIdAsync(id);
                    if (updatedItem != null && existingItem != null)
                        runOnUiThread(() =>
                        {
                            var index = collection.IndexOf(existingItem);
                            if (index >= 0)
                                collection[index] = updatedItem;
                        });
                    break;

                case "DELETE":
                    if (existingItem != null)
                        runOnUiThread(() => collection.Remove(existingItem));
                    break;
            }
        });
    }
}