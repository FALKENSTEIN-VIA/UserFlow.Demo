/// *****************************************************************************************
/// @file IDashboardService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for admin dashboard statistics and user import/export operations.
/// @details
/// Provides methods to retrieve high-level system statistics and handle import/export of user data.
/// Used by admin views to display aggregated metrics and manage user data efficiently.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 📊 Interface for accessing dashboard metrics and performing user import/export.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// 👥 Returns the total number of users in the system.
    /// </summary>
    /// <returns>Total user count as integer.</returns>
    Task<int> GetUserCountAsync();

    /// <summary>
    /// 📦 Returns the total number of projects in the system.
    /// </summary>
    /// <returns>Total project count as integer.</returns>
    Task<int> GetProjectCountAsync();

    /// <summary>
    /// 🆕 Retrieves the most recently registered users.
    /// </summary>
    /// <param name="count">Number of users to return (default is 5).</param>
    /// <returns>List of recent <see cref="UserDTO"/> users or null.</returns>
    Task<IEnumerable<UserDTO>?> GetLatestUsersAsync(int count = 5);

    /// <summary>
    /// 📥 Imports user records from a CSV file.
    /// </summary>
    /// <param name="file">Uploaded CSV file containing user data.</param>
    /// <returns>Import result containing success/failure details or null on error.</returns>
    Task<BulkOperationResultDTO<UserDTO>?> ImportUsersAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all users into a downloadable CSV file stream.
    /// </summary>
    /// <returns>Stream representing the exported file or null on failure.</returns>
    Task<Stream?> ExportUsersAsync();
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 📊 Useful for real-time admin dashboards (e.g., charts, KPIs).
/// - 📥 Supports user onboarding via CSV import (e.g., from HR tools).
/// - 📤 Export allows secure backup and migration of user datasets.
/// - ⚠️ Ensure proper file format and validation during import to avoid corrupt records.
/// - 🛡️ Access to this interface should be restricted to admin roles only.
/// </remarks>
