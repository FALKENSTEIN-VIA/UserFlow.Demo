/// @file SecurityExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides security-related query filters for Entity Framework Core.
/// @details
/// Contains extension methods that help enforce multi-tenancy rules by filtering data
/// based on the current user's CompanyId and role (e.g., disallowing cross-company access).

using UserFlow.API.Data.Entities;

namespace UserFlow.API.Extensions;

/// <summary>
/// 👉 ✨ Provides security-based query filters (e.g., multi-tenancy enforcement).
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// 👉 ✨ Applies a company-based filter to restrict users to their own company data.
    /// </summary>
    /// <param name="query">The base query to apply the security filter on.</param>
    /// <param name="currentUser">The currently authenticated user.</param>
    /// <returns>The filtered query that enforces company-level data access.</returns>
    public static IQueryable<User> ApplySecurityFilter(
        this IQueryable<User> query,
        User currentUser)
    {
        /// 🔒 Restrict to own company if user is not the GlobalAdmin (ID = 1)
        if (currentUser.CompanyId.HasValue && currentUser.Id != 1)
        {
            return query.Where(u => u.CompanyId == currentUser.CompanyId); // 🔐 Only access users in same company
        }

        /// ✅ GlobalAdmin or system context — no filter applied
        return query;
    }
}

/// @remarks
/// Developer Notes:
/// - 🔐 Enforces that regular users only see users from their own company.
/// - 🛡️ GlobalAdmin (User ID = 1) is exempt from filtering and sees all data.
/// - 🧠 Useful in admin/user controllers to prevent unauthorized cross-company access.
/// - ✨ Extendable to other entities with CompanyId or custom rules.
/// - ⚠️ Should be used together with role checks for robust authorization handling.
