/// @file QueryExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Contains extension methods for applying dynamic Include clauses to EF Core queries.
/// @details
/// Allows the caller to specify which related entities should be included in the query dynamically,
/// based on a list of allowed include strings and the target DTO or entity projection.

using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Provides dynamic extension methods for EF Core query customization.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// 👉 ✨ Applies allowed `Include(...)` clauses dynamically based on string values.
    /// </summary>
    /// <typeparam name="T">The EF Core entity type.</typeparam>
    /// <param name="query">The base query to apply includes to.</param>
    /// <param name="includes">The list of requested includes from the caller.</param>
    /// <param name="allowedIncludes">The list of includes that are allowed to be used.</param>
    /// <returns>The modified query with the requested includes applied.</returns>
    public static IQueryable<T> ApplyIncludes<T>(
        this IQueryable<T> query,
        IEnumerable<string> includes,
        IEnumerable<string> allowedIncludes) where T : class
    {
        /// ⚠️ Return the original query if no includes are provided
        if (includes == null || !includes.Any()) return query;

        /// ✅ Filter out only allowed include strings (case-insensitive, distinct)
        var validIncludes = includes
            .Where(i => allowedIncludes.Contains(i, StringComparer.OrdinalIgnoreCase))
            .Distinct();

        /// 🔁 Apply each valid include string to the query
        foreach (var include in validIncludes)
        {
            switch (include.ToLowerInvariant())
            {
                case "company":
                    /// 🏢 Dynamically include Company navigation property (e.g., for User entity)
                    if (typeof(T) == typeof(User))
                        query = query.Include("Company"); // Use string-based Include
                    break;

                    /// ➕ Add additional case blocks here for more dynamic includes
            }
        }

        return query; // 🔁 Return the final query with includes applied
    }
}

/// @remarks
/// Developer Notes:
/// - 🧩 Enables dynamic `Include(...)` logic based on string inputs and validation rules.
/// - 🛡️ Only allowed includes are applied (for security and performance).
/// - 🧠 Useful for admin dashboards or client-side filtered queries (e.g., ?include=company).
/// - 📦 `typeof(T)` checks can be extended to support multiple entity types dynamically.
/// - ⚠️ Avoid overusing string-based includes in performance-critical paths.
/// - ✨ Consider migrating to strongly typed includes or compiled expressions for full control.
