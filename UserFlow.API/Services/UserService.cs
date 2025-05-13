/// @file UserService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-28
/// @brief Handles creation of Identity Users for employees.
/// @details
/// Provides helper methods to create a User account with password and role assignment.
/// Uses ASP.NET Core Identity for managing users and assigning them to roles.

using Microsoft.AspNetCore.Identity;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Services
{
    /// <summary>
    /// 👉 ✨ Provides logic for creating Identity users and assigning them roles.
    /// </summary>
    public class UserService : IUserService
    {
        /// <summary>
        /// 🧱 Manages user persistence and Identity operations.
        /// </summary>
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// 👉 ✨ Constructor injecting <see cref="UserManager{User}"/>.
        /// </summary>
        /// <param name="userManager">ASP.NET Identity user manager.</param>
        public UserService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        /// <inheritdoc/>
        /// <summary>
        /// 🚀 Creates a new Identity user with email, password, and optional role.
        /// </summary>
        /// <param name="email">📧 Email of the user (used as username).</param>
        /// <param name="password">🔐 Password to assign to the user.</param>
        /// <param name="role">🛡️ Optional role to assign (e.g. "Admin").</param>
        /// <returns>The created <see cref="User"/> object.</returns>
        /// <exception cref="ApplicationException">Thrown when user creation fails.</exception>
        public async Task<User> CreateIdentityUserAsync(string email, string password, string role)
        {
            /// 🧑 Create a new user object
            var user = new User
            {
                UserName = email,
                Email = email
            };

            /// 💾 Attempt to persist the user
            var result = await _userManager.CreateAsync(user, password);

            /// ❌ If creation failed, throw detailed exception
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            /// 🛡️ Assign role if provided
            if (!string.IsNullOrEmpty(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            return user;
        }
    }
}

/// @remarks
/// Developer Notes:
/// - 🛠 This service encapsulates user creation logic to avoid duplicating UserManager calls.
/// - ✅ Assigns the provided password and optionally links a role via Identity RoleManager.
/// - 🧪 Ensure that roles exist before calling `AddToRoleAsync`, otherwise the operation will fail silently.
/// - 💥 Exception handling ensures caller is notified of user creation problems (e.g., duplicate email, weak password).
/// - 🧼 Can be extended to support additional profile setup steps (e.g., name, company binding).
