/// *****************************************************************************************
/// @file ISoftDelete.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Interface for entities supporting soft delete functionality.
/// *****************************************************************************************

namespace UserFlow.API.Shared.SoftDelete
{
    /// <summary>
    /// 🧹✨ Interface for entities that support soft delete functionality.
    /// </summary>
    /// <remarks>
    /// This interface defines a common contract for entities that can be marked as "soft deleted"
    /// rather than being permanently removed from the database.
    /// </remarks>
    public interface ISoftDelete
    {
        /// <summary>
        /// 🗑️ Indicates whether the entity is logically deleted.
        /// </summary>
        /// <value>
        /// `true` if the entity is marked as deleted and should be excluded from most queries;
        /// `false` if the entity is active and visible in query results.
        /// </value>
        bool IsDeleted { get; set; }
    }
}

/// @remarks
/// 🛠️ **Developer Notes**
/// 
/// - This interface is used in conjunction with EF Core's `HasQueryFilter(...)` method to automatically
///   exclude soft-deleted records from query results.
/// - Entities implementing `ISoftDelete` typically use this flag instead of permanently deleting rows from the database.
/// - For example, in `OnModelCreating`:  
///   ```csharp
///   modelBuilder.Entity<Foo>().HasQueryFilter(f => !f.IsDeleted);
///   ```
/// 
/// ✅ **Benefits**
/// - Preserves historical data for auditing or recovery.
/// - Allows users to "restore" deleted items later.
/// 
/// 📌 **Related Concepts**
/// - Combine with `BaseEntity` or `IAuditable` for full lifecycle tracking.
/// - Often paired with `DeletedAt`, `DeletedBy` for extended audit information.
/// 
/// *****************************************************************************************
