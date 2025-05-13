/// @file ICurrentUserService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Interface for accessing current logged-in user information.
/// @details
/// Provides user-specific context such as ID and role membership,
/// used throughout the application to enforce multi-tenancy and authorization checks.

namespace UserFlow.API.Services.Interfaces;

/// <summary>
/// 👉 ✨ Provides the current user's identity and role context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 👉 ✨ Gets the ID of the currently authenticated user.
    /// </summary>
    long UserId { get; }

    /// <summary>
    /// 👉 ✨ Gets the company ID assigned to the current user (if any).
    /// </summary>
    long? CompanyId { get; }

    /// <summary>
    /// 👉 ✨ Checks whether the current user belongs to the specified role.
    /// </summary>
    /// <param name="role">The role name to check (e.g. "Admin", "User").</param>
    /// <returns><c>true</c> if the user is in the role; otherwise <c>false</c>.</returns>
    bool IsInRole(string role);
}

/// @remarks
/// Developer Notes:
/// - 🧠 This interface is injected into services/controllers to access the logged-in user's identity.
/// - 🔐 Critical for enforcing multi-tenancy via `UserId` and `CompanyId` filters.
/// - 🔎 The `IsInRole` method supports role-based authorization logic across the app.
/// - ⚙️ Implementation (e.g., CurrentUserService) should extract data from the HttpContext safely.
/// - 📦 Keep logic stateless: this interface only exposes values from the current request context.
