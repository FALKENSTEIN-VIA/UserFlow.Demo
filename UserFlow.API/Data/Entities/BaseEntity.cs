/// @file BaseEntity.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Defines common base properties for all entities.
/// @details
/// Provides a standardized structure with primary key, soft delete flag, timestamps
/// (CreatedAt, UpdatedAt), and audit fields (CreatedBy, UpdatedBy) for consistency.

using System.ComponentModel.DataAnnotations;

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 🧱 Abstract base class for all entities.
/// Defines consistent structure for primary key, timestamps, soft deletion and audit fields.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 🔑 Primary key (unique identifier for each record).
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 🗑 Indicates if the entity is soft-deleted (not physically removed).
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 📅 Date and time when the entity was created (UTC).
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// 📅 Date and time when the entity was last updated (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 👤 UserId of the user who created this entity.
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 👤 UserId of the user who last modified this entity.
    /// </summary>
    public long? UpdatedBy { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 🔄 Inherit this class in all database entities for consistent tracking.
/// - 🗑 `IsDeleted` is used for soft deletion with global query filters in DbContext.
/// - 📅 `CreatedAt` and `UpdatedAt` support audit trails and UI sorting.
/// - 👤 `CreatedBy` and `UpdatedBy` help with accountability and change history.
/// - ⚠️ Always populate audit fields via service layer or middleware (not manually).
/// - 🧠 You can override this base class if you need specialized behavior per entity.
