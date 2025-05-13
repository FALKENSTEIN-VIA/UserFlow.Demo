/// @file IUserService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Interface for creating new identity users in the system.
/// @details
/// Provides a contract for user creation logic that includes setting email, password, and role.
/// Used by the authentication and admin registration flow to create new identity records.

using UserFlow.API.Data.Entities;

namespace UserFlow.API.Services;

/// <summary>
/// 👉 ✨ Service interface for identity user creation.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 👉 ✨ Creates a new user in the identity system.
    /// </summary>
    /// <param name="email">📧 Email address for the new user.</param>
    /// <param name="password">🔐 Password to be assigned.</param>
    /// <param name="role">🛡️ Role to assign (e.g. "User", "Admin").</param>
    /// <returns>The newly created <see cref="User"/> entity.</returns>
    /// <remarks>
    /// - ✅ Used in both registration and admin user creation flows.
    /// - 🔐 Password is hashed and stored securely.
    /// - 🧠 The returned User can be used for further customization (e.g., company assignment).
    /// </remarks>
    Task<User> CreateIdentityUserAsync(string email, string password, string role);
}

/// @remarks
/// Developer Notes:
/// - 🏗️ This service abstracts Identity user creation logic.
/// - 🔁 Helps decouple user setup from controller or seeder logic.
/// - 📦 May include email normalization, role assignment, and additional checks internally.
/// - 🧱 Extend this interface if you later want to support bulk user creation or invitation workflows.
