/// @file StaticCurrentUserService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Provides static access to the current user ID for use in EF Core global query filters.
/// @details
/// This static service allows the current user ID to be accessed globally, particularly for use in EF Core
/// global query filters (e.g., `HasQueryFilter`) to support multi-tenancy by filtering data based on the current user's ID.
/// The `StaticUserId` property must be set manually before any EF Core queries are executed within a given request context.
/// It should be cleared after each request to avoid data leaks between requests.

namespace UserFlow.API.Services
{
    /// <summary>
    /// 👉 ✨ Static service to hold the current user ID for EF Core query filtering.
    /// </summary>
    /// <remarks>
    /// 👉 Used inside EF Core's `HasQueryFilter` expressions to enforce user-level data access restrictions.
    /// </remarks>
    public static class StaticCurrentUserService
    {
        /// <summary>
        /// 🔐 Stores the ID of the current authenticated user for EF Core to filter queries.
        /// </summary>
        /// <remarks>
        /// ⚠️ This must be explicitly set at the beginning of each request and reset afterward.
        /// </remarks>
        public static long? StaticUserId { get; set; }
    }
}

/// @remarks
/// Developer Notes:
/// - 🛡 Enables multi-tenancy by exposing the current user ID to static EF Core query filters.
/// - 🚫 EF Core query filters do not support constructor injection — this static approach solves that limitation.
/// - ✅ Always set `StaticUserId` before executing database operations (e.g., in middleware).
/// - 🧼 Clear the value after the request to avoid cross-request leaks in a shared/static context.
/// - ⚙️ Used in `OnModelCreating` for `HasQueryFilter(...)` expressions like `e => e.UserId == StaticCurrentUserService.StaticUserId`.
