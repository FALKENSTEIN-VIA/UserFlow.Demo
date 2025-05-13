/// *****************************************************************************************
/// @file IScreenActionService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for managing screen actions via API.
/// @details
/// Provides methods to perform CRUD operations, filtering, bulk import/export,
/// and restoration of soft-deleted screen actions. Supports multi-dimensional queries (by screen, project, user, type).
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 📲 Interface for managing screen actions via the UserFlow API.
/// </summary>
public interface IScreenActionService
{
    /// <summary>
    /// 📄 Retrieves all screen actions for the current context (user or company).
    /// </summary>
    Task<IEnumerable<ScreenActionDTO>?> GetAllAsync();

    /// <summary>
    /// 🔍 Gets a single screen action by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the screen action.</param>
    Task<ScreenActionDTO?> GetByIdAsync(long id);

    /// <summary>
    /// 🆕 Creates a new screen action.
    /// </summary>
    /// <param name="dto">The screen action creation data.</param>
    Task<ScreenActionDTO?> CreateAsync(ScreenActionCreateDTO dto);

    /// <summary>
    /// ✏️ Updates an existing screen action.
    /// </summary>
    /// <param name="id">The ID of the screen action to update.</param>
    /// <param name="dto">The updated screen action data.</param>
    Task<bool> UpdateAsync(long id, ScreenActionUpdateDTO dto);

    /// <summary>
    /// 🗑️ Soft-deletes a screen action by ID.
    /// </summary>
    /// <param name="id">The ID of the screen action to delete.</param>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// ♻️ Restores a previously deleted screen action.
    /// </summary>
    /// <param name="id">The ID of the screen action to restore.</param>
    Task<ScreenActionDTO?> RestoreAsync(long id);

    /// <summary>
    /// 🔎 Retrieves all screen actions associated with a specific project.
    /// </summary>
    /// <param name="projectId">The project ID to filter by.</param>
    Task<IEnumerable<ScreenActionDTO>?> GetByProjectAsync(long projectId);

    /// <summary>
    /// 🖥️ Retrieves all screen actions associated with a specific screen.
    /// </summary>
    /// <param name="screenId">The screen ID to filter by.</param>
    Task<IEnumerable<ScreenActionDTO>?> GetByScreenAsync(long screenId);

    /// <summary>
    /// 👤 Retrieves all screen actions created by a specific user.
    /// </summary>
    /// <param name="userId">The user ID to filter by.</param>
    Task<IEnumerable<ScreenActionDTO>?> GetByUserAsync(long userId);

    /// <summary>
    /// 🏷️ Retrieves all screen actions of a specific action type.
    /// </summary>
    /// <param name="typeId">The type ID to filter by (e.g., click, swipe).</param>
    Task<IEnumerable<ScreenActionDTO>?> GetByTypeAsync(long typeId);

    /// <summary>
    /// 📥 Imports screen actions from a CSV file.
    /// </summary>
    /// <param name="file">The uploaded CSV file.</param>
    Task<BulkOperationResultDTO<ScreenActionDTO>?> ImportAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all screen actions to a downloadable CSV stream.
    /// </summary>
    Task<Stream?> ExportAsync();
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 🔄 Supports soft delete and restore to avoid permanent data loss.
/// - 🔍 Filtering by project, screen, user, or action type supports contextual UIs.
/// - 📥 CSV import supports bulk input with server-side validation (via CsvHelper).
/// - 📤 CSV export supports backups, reporting, and offline analysis.
/// - 🛡️ Multi-tenancy must be enforced through token-scoped context resolution.
/// - 🧪 Logging and structured fallback responses are crucial for UI feedback.
/// - ✅ Always validate DTOs before calling create/update to ensure integrity.
/// </remarks>
