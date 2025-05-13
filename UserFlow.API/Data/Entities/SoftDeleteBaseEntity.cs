/// @file SoftDeleteBaseEntity.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Abstract base entity supporting audit fields and soft delete functionality.
/// @details
/// Provides a standardized structure for common entity metadata:
/// - Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
/// - Soft delete capability (IsDeleted flag)
/// - Multi-tenancy support via UserId association.

using UserFlow.API.Data.Interfaces;

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Abstract base entity supporting audit fields, soft delete functionality, and multi-tenancy.
/// </summary>
public abstract class SoftDeleteBaseEntity : ISoftDelete, IUserOwned
{
    /// <summary>
    /// 🔑 Unique identifier (primary key) of the entity.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🕓 UTC timestamp when the entity was initially created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 👤 ID of the user who created the entity.
    /// </summary>
    public long CreatedBy { get; set; }

    /// <summary>
    /// 🕓 UTC timestamp when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 👤 ID of the user who last updated the entity.
    /// </summary>
    public long? UpdatedBy { get; set; }

    /// <summary>
    /// 🗑 Indicates whether the entity is soft-deleted (logically deleted but still present in database).
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 🏷️ ID of the user who owns the entity, supporting multi-tenant scenarios.
    /// </summary>
    public long UserId { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 🧠 Use as base for all entities that require soft delete and audit tracking.
/// - 🏢 Supports multi-tenancy by including a UserId property.
/// - 🛡 Ensure query filters are applied (HasQueryFilter) in DbContext to respect IsDeleted and UserId.
/// - 📦 Can be extended to include CompanyId or other tenant boundaries as needed.
/// - ⚠️ Always initialize CreatedAt in constructors or during creation logic.
