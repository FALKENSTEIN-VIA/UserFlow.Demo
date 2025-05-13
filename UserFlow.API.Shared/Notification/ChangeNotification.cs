
using System.Text.Json.Serialization;

namespace UserFlow.API.Shared.Notifications;

public record ChangeNotification(
    [property: JsonPropertyName("entityName")] string EntityName,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("entityId")] string EntityId,
    [property: JsonPropertyName("changedAt")] DateTime ChangedAt
);
