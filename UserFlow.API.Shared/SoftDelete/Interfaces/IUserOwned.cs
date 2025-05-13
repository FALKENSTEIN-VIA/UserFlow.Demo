/// *****************************************************************************************
/// @file IUserOwned.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Interface for entities supporting multi-tenancy (user ownership).
/// *****************************************************************************************

namespace UserFlow.API.Shared.SoftDelete
{
    /// <summary>
    /// 👤✨ Interface for entities owned by a specific user (multi-tenancy).
    /// </summary>
    /// <remarks>
    /// Entities implementing this interface are associated with a single user, enabling
    /// data isolation based on the current user's identity.
    /// </remarks>
    public interface IUserOwned
    {
        /// <summary>
        /// 🔐 The ID of the user who owns this entity.
        /// </summary>
        /// <value>
        /// A unique long integer identifying the user associated with this entity.
        /// This value is typically matched against the current user's ID from the request context.
        /// </value>
        long UserId { get; set; }
    }
}

/// @remarks
/// 🛠️ **Developer Notes**
/// 
/// - Enables per-user data segregation in multi-tenant systems.
/// - Typically used with global query filters like:
///   ```csharp
///   modelBuilder.Entity<T>().HasQueryFilter(e => e.UserId == StaticCurrentUserService.StaticUserId);
///   ```
/// - Works in combination with `ICurrentUserService` or `StaticCurrentUserService`.
/// - Commonly implemented in user-owned entities such as: `Note`, `Project`, `Screen`, etc.
/// 
/// ✅ **Best Practices**
/// - Always ensure that write operations also verify ownership for security.
/// - Combine with `ISoftDelete` for safe deletion.
/// 
/// 🔗 **See Also**
/// - `ICompanyOwned` – for company-level ownership.
/// - `ICurrentUserService` – for retrieving current user ID.
/// 
/// *****************************************************************************************
