/// *****************************************************************************************
/// @file IScreenActionTypeService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for managing ScreenActionTypes via API.
/// @details
/// Provides methods to create, update, delete, restore, paginate, bulk import/export, 
/// and retrieve ScreenActionType records used for categorizing screen interactions.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 🏷️ Interface for managing screen action types via the UserFlow API.
/// </summary>
public interface IScreenActionTypeService
{
    /// <summary>
    /// 📄 Retrieves all available screen action types.
    /// </summary>
    /// <returns>A list of screen action types.</returns>
    Task<IEnumerable<ScreenActionTypeDTO>> GetAllAsync();

    /// <summary>
    /// 🆕 Creates a new screen action type entry.
    /// </summary>
    /// <param name="dto">The screen action type creation DTO.</param>
    /// <returns>The newly created screen action type.</returns>
    Task<ScreenActionTypeDTO> CreateAsync(ScreenActionTypeCreateDTO dto);

    /// <summary>
    /// ✏️ Updates an existing screen action type.
    /// </summary>
    /// <param name="id">The ID of the screen action type to update.</param>
    /// <param name="dto">The updated screen action type data.</param>
    /// <returns>True if update succeeded.</returns>
    Task<bool> UpdateAsync(long id, ScreenActionTypeUpdateDTO dto);

    /// <summary>
    /// 🗑️ Soft-deletes a screen action type.
    /// </summary>
    /// <param name="id">The ID of the screen action type to delete.</param>
    /// <returns>True if deletion succeeded.</returns>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// ♻️ Restores a previously deleted screen action type.
    /// </summary>
    /// <param name="id">The ID of the screen action type to restore.</param>
    /// <returns>The restored screen action type DTO if successful.</returns>
    Task<ScreenActionTypeDTO?> RestoreAsync(long id);

    /// <summary>
    /// 📦 Creates multiple screen action types in bulk.
    /// </summary>
    /// <param name="list">List of DTOs to create.</param>
    /// <returns>Bulk operation result with imported/failed info.</returns>
    Task<BulkOperationResultDTO<ScreenActionTypeDTO>> BulkCreateAsync(List<ScreenActionTypeCreateDTO> list);

    /// <summary>
    /// 📊 Retrieves paginated screen action types.
    /// </summary>
    /// <param name="page">Page number (starting at 1).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <returns>Paged result containing screen action type DTOs.</returns>
    Task<PagedResultDTO<ScreenActionTypeDTO>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// 📥 Imports screen action types from a CSV file.
    /// </summary>
    /// <param name="file">CSV file stream.</param>
    /// <returns>Bulk operation result after import.</returns>
    Task<BulkOperationResultDTO<ScreenActionTypeDTO>> ImportAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all screen action types as a downloadable CSV file.
    /// </summary>
    /// <returns>Stream containing CSV data.</returns>
    Task<Stream> ExportAsync();
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 🏷️ Action types are used to classify user interactions (e.g., click, swipe).
/// - 🔄 Soft delete & restore allow non-destructive data handling.
/// - 📦 Bulk operations simplify mass management from admin UIs or automation.
/// - 📥 Import requires structured CSV files validated by `ScreenActionTypeImportMap`.
/// - 📤 Export produces standardized output useful for auditing or backups.
/// - 📊 Paging improves performance and UI responsiveness in large datasets.
/// - 🔐 Role-based authorization is required for modifying data (admin only).
/// </remarks>
