/// *****************************************************************************************
/// @file IProjectService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for accessing and managing project-related API operations.
/// @details
/// Provides methods to create, read, update, delete, restore, import, and export projects.
/// Includes support for pagination, bulk operations, and soft deletion handling.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 🧱 Interface for accessing project-related API functionality (CRUD, bulk, import/export).
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// 📄 Retrieves all accessible projects (owned or shared).
    /// </summary>
    /// <returns>Enumerable list of <see cref="ProjectDTO"/>.</returns>
    Task<IEnumerable<ProjectDTO>> GetAllAsync();

    /// <summary>
    /// 🔍 Gets a specific project by its unique identifier.
    /// </summary>
    /// <param name="id">The ID of the project.</param>
    /// <returns><see cref="ProjectDTO"/> if found; otherwise null.</returns>
    Task<ProjectDTO?> GetByIdAsync(long id);

    /// <summary>
    /// 🆕 Creates a new project entry in the system.
    /// </summary>
    /// <param name="dto">The project creation data.</param>
    /// <returns>The newly created <see cref="ProjectDTO"/>.</returns>
    Task<ProjectDTO> CreateAsync(ProjectCreateDTO dto);

    /// <summary>
    /// ✏️ Updates an existing project.
    /// </summary>
    /// <param name="id">The ID of the project to update.</param>
    /// <param name="dto">The updated project data.</param>
    /// <returns>True if update was successful; otherwise false.</returns>
    Task<bool> UpdateAsync(long id, ProjectUpdateDTO dto);

    /// <summary>
    /// 🗑️ Soft-deletes a project by ID.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    /// <returns>True if deletion was successful; otherwise false.</returns>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// ♻️ Restores a previously deleted project.
    /// </summary>
    /// <param name="id">The ID of the project to restore.</param>
    /// <returns>Restored <see cref="ProjectDTO"/> or null if not found.</returns>
    Task<ProjectDTO?> RestoreAsync(long id);

    /// <summary>
    /// 📦 Bulk creates projects from a given list.
    /// </summary>
    /// <param name="list">List of project creation DTOs.</param>
    /// <returns>A <see cref="BulkOperationResultDTO{T}"/> with import details.</returns>
    Task<BulkOperationResultDTO<ProjectDTO>> BulkCreateAsync(List<ProjectCreateDTO> list);

    /// <summary>
    /// 🔢 Returns paginated projects with metadata.
    /// </summary>
    /// <param name="page">Page number starting from 1.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paged result of <see cref="ProjectDTO"/>.</returns>
    Task<PagedResultDTO<ProjectDTO>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// 📥 Imports projects from a CSV file.
    /// </summary>
    /// <param name="file">CSV file to upload.</param>
    /// <returns>Bulk operation result with success/failure details.</returns>
    Task<BulkOperationResultDTO<ProjectDTO>> ImportAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all projects as a CSV stream.
    /// </summary>
    /// <returns>Stream containing CSV-formatted project data.</returns>
    Task<Stream> ExportAsync();
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 📦 Supports soft delete, restore, and all standard CRUD operations.
/// - 🔁 Import/export use CSV and `MultipartFormDataContent`.
/// - 🔢 Pagination allows UI efficiency on large datasets.
/// - 🤝 Bulk creation is optimized for onboarding.
/// - 🛡 Role-based access and company scoping must be enforced by the API.
/// - 🔐 Token handling via <see cref="AuthorizedHttpClient"/> is assumed.
/// </remarks>
