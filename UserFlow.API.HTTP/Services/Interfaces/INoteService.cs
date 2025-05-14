/// *****************************************************************************************
/// @file INoteService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for managing notes (CRUD, restore, import/export) via API.
/// @details
/// Provides methods for creating, updating, deleting, restoring, and importing/exporting notes.
/// Includes support for soft deletion and CSV file-based bulk operations.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 📝 Interface for managing notes via the UserFlow API (CRUD, restore, import/export).
/// </summary>
public interface INoteService
{
    /// <summary>
    /// 📄 Retrieves all notes for the current user or company context.
    /// </summary>
    /// <returns>List of <see cref="NoteDTO"/> or null if none exist.</returns>
    Task<IEnumerable<NoteDTO>?> GetAllAsync();

    /// <summary>
    /// 📄 Retrieves a specific note by its ID.  
    /// </summary>
    /// <param name="id"></param>
    /// <returns>A single NoteDTO or null if not found.</returns>
    Task<NoteDTO?> GetByIdAsync(long id);

    /// <summary>
    /// 🆕 Creates a new note.
    /// </summary>
    /// <param name="dto">The <see cref="NoteDTO"/> containing note data.</param>
    /// <returns>The created note as <see cref="NoteDTO"/> or null if failed.</returns>
    Task<NoteDTO?> CreateAsync(NoteDTO dto);

    /// <summary>
    /// ✏️ Updates an existing note by ID.
    /// </summary>
    /// <param name="id">The note ID to update.</param>
    /// <param name="dto">The updated note data.</param>
    /// <returns>True if update was successful, otherwise false.</returns>
    Task<bool> UpdateAsync(long id, NoteDTO dto);

    /// <summary>
    /// 🗑️ Soft-deletes a note by ID.
    /// </summary>
    /// <param name="id">The ID of the note to delete.</param>
    /// <returns>True if deletion was successful, otherwise false.</returns>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// ♻️ Restores a previously deleted note by ID.
    /// </summary>
    /// <param name="id">The ID of the note to restore.</param>
    /// <returns>The restored <see cref="NoteDTO"/> or null.</returns>
    Task<NoteDTO?> RestoreAsync(long id);

    /// <summary>
    /// 📤 Exports all notes as a downloadable CSV file.
    /// </summary>
    /// <returns>CSV stream containing notes.</returns>
    Task<Stream?> ExportAsync();

    /// <summary>
    /// 📥 Imports notes from a CSV file.
    /// </summary>
    /// <param name="file">CSV file containing notes.</param>
    /// <returns>Bulk result of the import with success and error details.</returns>
    Task<BulkOperationResultDTO<NoteDTO>?> ImportAsync(IFormFile file);
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 📝 Notes are bound to users and companies (multi-tenancy).
/// - 🔁 Supports full CRUD lifecycle including soft delete and restore.
/// - 📥 CSV import requires proper formatting and headers.
/// - 📤 Export guarantees standardized schema.
/// - 🛡 Always validate and authorize ownership of each note.
/// - 🧪 Uses DTOs to decouple from persistence models.
/// </remarks>
