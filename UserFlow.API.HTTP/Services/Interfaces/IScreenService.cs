/// *****************************************************************************************
/// @file IScreenService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for accessing screen-related API endpoints.
/// @details
/// Defines operations to create, update, delete, restore, paginate, import, and export screen data.
/// Screens represent individual UI states or views in a project and are linked to projects and companies.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 📺 Interface for accessing screen-related API endpoints.
/// </summary>
public interface IScreenService
{
    /// <summary>
    /// 📄 Retrieves all screens accessible to the current user.
    /// </summary>
    Task<IEnumerable<ScreenDTO>> GetAllAsync();

    /// <summary>
    /// 🔍 Retrieves all screens belonging to a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    Task<IEnumerable<ScreenDTO>> GetByProjectIdAsync(long projectId);

    /// <summary>
    /// 🆕 Creates a new screen and returns validation feedback.
    /// </summary>
    /// <param name="dto">The screen creation DTO.</param>
    Task<ValidationResponseDTO> CreateAsync(ScreenCreateDTO dto);

    /// <summary>
    /// ✏️ Updates an existing screen and returns validation feedback.
    /// </summary>
    /// <param name="id">The ID of the screen to update.</param>
    /// <param name="dto">The update DTO with new screen values.</param>
    Task<ValidationResponseDTO> UpdateAsync(long id, ScreenUpdateDTO dto);

    /// <summary>
    /// 🗑️ Soft-deletes a screen by its ID.
    /// </summary>
    /// <param name="id">The ID of the screen to delete.</param>
    /// <returns>True if the operation succeeded.</returns>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// ♻️ Restores a previously deleted screen by its ID.
    /// </summary>
    /// <param name="id">The ID of the screen to restore.</param>
    /// <returns>The restored screen DTO if successful.</returns>
    Task<ScreenDTO?> RestoreAsync(long id);

    /// <summary>
    /// 📦 Creates multiple screens in a single operation (bulk insert).
    /// </summary>
    /// <param name="list">List of screen DTOs to create.</param>
    /// <returns>A result containing created screens and any import errors.</returns>
    Task<BulkOperationResultDTO<ScreenDTO>> BulkCreateAsync(List<ScreenCreateDTO> list);

    /// <summary>
    /// 📊 Retrieves paginated screens for dashboards or large result sets.
    /// </summary>
    /// <param name="page">The page number to retrieve (starting from 1).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>Paged result containing screens and metadata.</returns>
    Task<PagedResultDTO<ScreenDTO>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// 📥 Imports screens from a CSV or Excel file.
    /// </summary>
    /// <param name="file">The uploaded file containing screen definitions.</param>
    /// <returns>Bulk operation result including successes and failures.</returns>
    Task<BulkOperationResultDTO<ScreenDTO>> ImportAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all available screens into a downloadable stream (CSV format).
    /// </summary>
    /// <returns>A stream containing the exported file.</returns>
    Task<Stream> ExportAsync();
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 📺 Screens represent discrete views or states in a UI flow.
/// - 🔁 Supports soft delete, restore, bulk, pagination, import/export.
/// - 📤 Exports are ideal for reporting or data archival.
/// - 📥 Imports require strict formatting, validated server-side.
/// - ✅ Uses DTOs for strong typing and separation from EF entities.
/// - 🔐 All methods rely on token-based authorization via `AuthorizedHttpClient`.
/// </remarks>
