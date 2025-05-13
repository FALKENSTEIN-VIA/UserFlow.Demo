/// *****************************************************************************************
/// @file BulkDeleteRequest.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO for requesting the bulk deletion of multiple entities.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 🧹 DTO for bulk deletion of multiple entities using their IDs.
/// </summary>
/// <remarks>
/// This DTO is typically used with endpoints that implement batch delete operations, usually via soft delete.
/// </remarks>
public class BulkDeleteRequest
{
    /// <summary>
    /// 🆔 List of entity IDs that should be deleted (soft delete).
    /// </summary>
    /// <value>
    /// A list of unique identifiers of the entities to be marked as deleted.
    /// </value>
    public List<long> Ids { get; set; } = new();
}

/// @remarks
/// 🛠️ **Developer Notes**
/// - This DTO is used in API endpoints like `DELETE /api/[controller]/bulk`.
/// - Supports soft deletion patterns—actual data is not removed from the database, but marked as deleted.
/// - Always validate that the user has access to the listed IDs before processing deletion.
/// - This DTO enables improved API performance by allowing multiple delete operations in one request.
///
/// 🚧 **Usage Example**
/// ```json
/// {
///   "ids": [101, 102, 103]
/// }
/// ```
/// *****************************************************************************************
