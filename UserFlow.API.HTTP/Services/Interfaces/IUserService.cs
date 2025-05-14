/// *****************************************************************************************
/// @file IUserService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for managing users via the UserFlow API.
/// @details
/// Provides operations for retrieving, updating, deleting, creating, importing, and exporting user records,
/// including admin-specific functions for bulk management and account provisioning.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Interface for managing users via the UserFlow API.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 👥 Retrieves all users visible to the current user (admin-scoped or global).
    /// </summary>
    /// <returns>A list of <see cref="UserDTO"/> objects.</returns>
    Task<IEnumerable<UserDTO>> GetAllAsync(bool includeCompany = false);

    /// <summary>
    /// 🔍 Retrieves a specific user by their ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>A <see cref="UserDTO"/> if found, or null.</returns>
    Task<UserDTO?> GetByIdAsync(long id, bool includeCompany = false);

    /// <summary>
    /// 📝 Updates a user with the given profile data.
    /// </summary>
    /// <param name="dto">User update data.</param>
    /// <returns>True if update was successful.</returns>
    Task<bool> UpdateAsync(UserUpdateDTO dto);

    /// <summary>
    /// 🗑️ Soft deletes the specified user by ID.
    /// </summary>
    /// <param name="id">User ID to delete.</param>
    /// <returns>True if deletion was successful.</returns>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 🧑‍💼 Creates a new user as an admin (pre-registration flow).
    /// </summary>
    /// <param name="dto">Admin-level user creation DTO.</param>
    /// <returns>True if creation was successful.</returns>
    Task<bool> CreateByAdminAsync(UserCreateByAdminDTO dto);

    /// <summary>
    /// 📦 Bulk creates multiple users at once (admin-only).
    /// </summary>
    /// <param name="dtos">List of user creation DTOs.</param>
    /// <returns>A bulk operation result indicating success and error details.</returns>
    Task<BulkOperationResultDTO<UserDTO>> BulkCreateAsync(List<UserCreateByAdminDTO> dtos);

    /// <summary>
    /// 📥 Imports users from a CSV file.
    /// </summary>
    /// <param name="file">CSV file containing user data.</param>
    /// <returns>A bulk result with import success/error counts.</returns>
    Task<BulkOperationResultDTO<UserDTO>> ImportAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all users to a downloadable CSV file (byte stream).
    /// </summary>
    /// <returns>CSV file as byte array.</returns>
    Task<byte[]> ExportAsync();

    /// <summary>
    /// ♻️ Restores a previously deleted user by ID.
    /// </summary>
    /// <param name="id">ID of the user to restore.</param>
    /// <returns>The restored <see cref="UserDTO"/>, or null if failed.</returns>
    Task<UserDTO?> RestoreUserAsync(long id);
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 👤 Admins can create and restore users using individual or bulk operations.
/// - 🗑 Deletions are soft, enabling data recovery via `RestoreUserAsync`.
/// - 📥 CSV import uses `CsvHelper` + `UserImportMap` for mapping.
/// - 📤 Export provides CSV byte array to support file downloads.
/// - 🔐 Role-based authorization should be enforced per method.
/// - 💡 Use `NeedsPasswordSetup` for user onboarding scenarios (e.g., set password on first login).
/// </remarks>
