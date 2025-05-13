/// *****************************************************************************************
/// @file BaseDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Abstract base class for all DTOs providing common metadata.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 🧱 Abstract base class for all DTOs.
/// </summary>
/// <remarks>
/// Provides shared metadata fields such as ID, timestamps, and soft-delete marker.
/// All entity-specific DTOs should inherit from this base to ensure consistency.
/// </remarks>
public abstract class BaseDTO : IEntityDTO<long>
{
    /// <summary>
    /// 🆔 Primary identifier of the DTO.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 📅 Timestamp of when the entity was created.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// ✏️ Timestamp of the most recent update.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 🗑️ Indicates whether the entity is marked as soft-deleted.
    /// </summary>
    public bool? IsDeleted { get; set; }
}

/// @remarks
/// 🛠️ **Developer Notes**
/// - All DTOs in the system should inherit from `BaseDTO` to include audit and lifecycle metadata.
/// - `Id` is the primary key identifier and maps to the entity’s database ID.
/// - `CreatedAt` and `UpdatedAt` help with auditing and change tracking.
/// - `IsDeleted` allows UI logic to handle soft-deleted entries gracefully (optional).
/// - Although `IsDeleted` is nullable for DTOs, in entities it's always `bool`.
/// *****************************************************************************************
