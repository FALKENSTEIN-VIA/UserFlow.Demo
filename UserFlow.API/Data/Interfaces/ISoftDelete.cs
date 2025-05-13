/// @file ISoftDelete.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Interface to indicate that an entity supports soft deletion (logical delete).
/// @details
/// Entities implementing this interface will use an IsDeleted flag to mark data as logically deleted
/// without physically removing it from the database.
/// Enables safer data management, historical tracking, and potential recovery scenarios.

namespace UserFlow.API.Data.Interfaces;

/// <summary>
/// 👉 ✨ Interface for entities that support soft deletion (logical delete).
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// 🗑 Indicates whether the entity is logically deleted (soft-deleted).
    /// </summary>
    bool IsDeleted { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 📦 Use in combination with EF Core global filters (e.g. HasQueryFilter).
/// - 💾 Promotes logical deletion instead of physical removal from the database.
/// - 🧠 Helps preserve historical integrity and enable recovery or restore flows.
/// - 🔁 Can be used together with ISoftRestorable for future "undelete" operations.
/// - 🚨 Ensure that related entities are either filtered or excluded via navigation configs (avoid cascade).
