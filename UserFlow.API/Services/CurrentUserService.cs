/// @file CurrentUserService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Provides access to the currently authenticated user's ID for multi-tenancy and security checks.
/// @details
/// Implements <see cref="ICurrentUserService"/> to retrieve user identity and claims from the current HTTP context.
/// Supports role checking and tenant-specific filtering by exposing user and company identifiers.

using System.Security.Claims;
using UserFlow.API.Services.Interfaces;

namespace UserFlow.API.Services;

/// <summary>
/// 👉 ✨ Service to retrieve current authenticated user's information from the HTTP context.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    /// <summary>
    /// 🌐 Stores reference to the accessor for HTTP context.
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>
    /// 🔑 Gets the authenticated user's ID from the claims.
    /// </summary>
    public long UserId =>
        long.Parse(_httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>
    /// 🏢 Gets the company ID associated with the current user (if available).
    /// </summary>
    public long? CompanyId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("CompanyId")
            is string companyId && long.TryParse(companyId, out var id)
            ? id : null;

    /// <summary>
    /// 🛡️ Checks whether the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role name to evaluate (e.g. "Admin").</param>
    /// <returns><c>true</c> if the user is in the role; otherwise <c>false</c>.</returns>
    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User
            .Claims
            .Any(c => c.Type == ClaimTypes.Role &&
                      c.Value.Equals(role, StringComparison.OrdinalIgnoreCase))
            ?? false;
    }
}

/// @remarks
/// Developer Notes:
/// - 🧠 This class reads claims injected into the JWT token after authentication.
/// - 🔐 Used for enforcing multi-tenancy (via CompanyId) and user-specific data access.
/// - ⚠️ Always ensure the NameIdentifier and CompanyId claims are correctly populated in the token.
/// - 🛡️ The `IsInRole` method supports custom role checks within services or controllers.
/// - ✅ Safe fallback is used for null HTTP context or missing claims.
