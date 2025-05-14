/// @file NoteController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-04
/// @brief API controller for managing notes including CRUD, bulk ops, CSV import/export, and soft delete.
/// @details
/// This controller provides endpoints for creating, reading, updating, deleting, restoring,
/// and importing/exporting note entries. It supports multi-tenancy, authorization,
/// and bulk operations for Admins and GlobalAdmins only.
///
/// @endpoints
/// - GET    /api/notes               → Get all notes (company filter unless GlobalAdmin)
/// - GET    /api/notes/{id}          → Get a single note by ID
/// - POST   /api/notes               → Create a new note (Admin/GlobalAdmin only)
/// - PUT    /api/notes/{id}          → Update a note by ID (Admin/GlobalAdmin only)
/// - DELETE /api/notes/{id}          → Soft delete a note (Admin/GlobalAdmin only)
/// - POST   /api/notes/{id}/restore  → Restore a soft-deleted note (Admin/GlobalAdmin only)
/// - GET    /api/notes/paged         → Paginated list of notes
/// - POST   /api/notes/bulk-create   → Bulk create notes (Admin/GlobalAdmin only)
/// - PUT    /api/notes/bulk-update   → Bulk update notes (Admin/GlobalAdmin only)
/// - POST   /api/notes/bulk-delete   → Bulk soft delete notes (Admin/GlobalAdmin only)
/// - POST   /api/notes/import        → Import notes from CSV file (Admin/GlobalAdmin only)
/// - GET    /api/notes/export        → Export notes to CSV (Admin/GlobalAdmin only)


using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using UserFlow.API.Data;
using UserFlow.API.Data.Entities;
using UserFlow.API.Mappers;
using UserFlow.API.Services.Interfaces;
using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.DTO.ImportMaps;

namespace UserFlow.API.Controllers;

/// <summary>
/// 📎 API controller for managing notes.
/// </summary>
[ApiController] // ✅ Enables model binding, validation, etc.
[Route("api/notes")] // 📍 Sets base route
[Authorize] // 🔐 Only authenticated users can access
public class NoteController : ControllerBase
{
    #region 🔒 Fields

    /// <summary>
    /// 🗃️ EF database context
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// 👤 Service for accessing the current user context
    /// </summary>
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// 📝 Logger instance
    /// </summary>
    private readonly ILogger<NoteController> _logger;

    #endregion

    #region 🔧 Constructor

    /// <summary>
    /// 🛠️ Constructor injecting dependencies
    /// </summary>
    public NoteController(AppDbContext db, ICurrentUserService currentUser, ILogger<NoteController> logger)
    {
        _context = db; // 🧱 Assign database context
        _currentUser = currentUser; // 🧱 Assign user context
        _logger = logger; // 🧱 Assign logger
    }

    #endregion

    #region 📄 CRUD

    /// <summary>
    /// 📄 Returns all notes for the current company (unless GlobalAdmin).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoteDTO>>> GetAllAsync()
    {
        _logger.LogInformation("📄 Getting all notes for user {UserId}...", _currentUser.UserId);

        var query = _context.Notes.AsQueryable(); // 🔍 Start query

        if (!_currentUser.IsInRole("GlobalAdmin")) // 🔐 Restrict by company
            query = query.Where(n => n.CompanyId == _currentUser.CompanyId);

        var result = await query
            .OrderByDescending(n => n.CreatedAt) // ⬇️ Latest first
            .Select(NoteMapper.ToNoteDto()) // 🔁 Project to DTO
            .ToListAsync();

        _logger.LogInformation("📄 Returned {Count} notes for user {UserId}.", result.Count, _currentUser.UserId);
        return Ok(result); // ✅ Return result
    }

    /// <summary>
    /// 🔎 Retrieves a note by ID.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<NoteDTO>> GetByIdAsync(long id)
    {
        _logger.LogInformation("🔍 Getting note by ID {Id} for user {UserId}.", id, _currentUser.UserId);

        var result = await _context.Notes
            .Where(n => n.Id == id)
            .Where(n => _currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId)
            .Select(NoteMapper.ToNoteDto())
            .FirstOrDefaultAsync(); // 🔍 Single result

        if (result == null)
        {
            _logger.LogWarning("❌ Note with ID {Id} not found or access denied.", id);
            return NotFound(); // ❌ Not found
        }

        _logger.LogInformation("✅ Note {Id} retrieved successfully.", id);
        return Ok(result); // ✅ Return DTO
    }

    /// <summary>
    /// ➕ Creates a new note.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<NoteDTO>> CreateAsync(NoteCreateDTO dto)
    {
        _logger.LogInformation("➕ Creating new note by user {UserId}.", _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ Cannot create note – no company assigned to user {UserId}.", _currentUser.UserId);
            return BadRequest("No company assigned to current user."); // ❌ Validation failed
        }

        var entity = new Note
        {
            Title = dto.Title,
            Content = dto.Content,
            ProjectId = dto.ProjectId,
            ScreenId = dto.ScreenId,
            ScreenActionId = dto.ScreenActionId,
            CompanyId = _currentUser.CompanyId.Value,
            UserId = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow, // 🕒 Timestamp
            CreatedBy = _currentUser.UserId // 👤 Author
        };

        _context.Notes.Add(entity); // 💾 Insert entity
        await _context.SaveChangesAsync(); // 💾 Save to DB

        var result = await _context.Notes
            .Where(n => n.Id == entity.Id)
            .Select(NoteMapper.ToNoteDto())
            .FirstAsync();

        _logger.LogInformation("✅ Note {Id} created successfully.", result.Id);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Id }, result); // ✅ Return created
    }

    /// <summary>
    /// ✏️ Updates a note.
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> UpdateAsync(long id, NoteUpdateDTO dto)
    {
        _logger.LogInformation("✏️ Updating note {Id} by user {UserId}.", id, _currentUser.UserId);

        var entity = await _context.Notes.FirstOrDefaultAsync(n =>
            n.Id == id && (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Note {Id} not found or not authorized for update.", id);
            return NotFound(); // ❌ Not found
        }

        entity.Title = dto.Title;
        entity.Content = dto.Content;
        entity.ProjectId = dto.ProjectId;
        entity.ScreenId = dto.ScreenId;
        entity.ScreenActionId = dto.ScreenActionId;
        entity.UpdatedAt = DateTime.UtcNow; // 🕒 Modified
        entity.UpdatedBy = _currentUser.UserId; // 👤 Modifier

        await _context.SaveChangesAsync(); // 💾 Save

        _logger.LogInformation("✅ Note {Id} updated successfully.", id);
        return NoContent(); // ✅ Success
    }

    /// <summary>
    /// 🗑️ Soft-deletes a note.
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        _logger.LogInformation("🗑️ Deleting note {Id} by user {UserId}.", id, _currentUser.UserId);

        var entity = await _context.Notes.FirstOrDefaultAsync(n =>
            n.Id == id && (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Note {Id} not found or not authorized for deletion.", id);
            return NotFound(); // ❌ Not found
        }

        entity.IsDeleted = true; // 🗑️ Soft delete
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(); // 💾 Save

        _logger.LogInformation("✅ Note {Id} soft-deleted successfully.", id);
        return NoContent(); // ✅ Success
    }

    /// <summary>
    /// ♻️ Restores a soft-deleted note.
    /// </summary>
    [HttpPost("{id:long}/restore")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> RestoreAsync(long id)
    {
        _logger.LogInformation("♻️ Restoring note {Id} by user {UserId}.", id, _currentUser.UserId);

        var entity = await _context.Notes
            .IgnoreQueryFilters() // ❗ Bypass soft delete filter
            .FirstOrDefaultAsync(n =>
                n.Id == id &&
                n.IsDeleted &&
                (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Note {Id} not found or not authorized for restore.", id);
            return NotFound(); // ❌ Not found
        }

        entity.IsDeleted = false; // ♻️ Restore
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(); // 💾 Save

        _logger.LogInformation("✅ Note {Id} restored successfully.", id);
        return NoContent(); // ✅ Done
    }

    /// <summary>
    /// 📄 Returns a paginated list of notes.
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDTO<NoteDTO>>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("📄 Getting paged notes (Page: {Page}, PageSize: {PageSize}) for user {UserId}.",
            page, pageSize, _currentUser.UserId);

        if (page < 1 || pageSize < 1 || pageSize > 200)
        {
            _logger.LogWarning("❌ Invalid pagination parameters (Page: {Page}, PageSize: {PageSize}).", page, pageSize);
            return BadRequest("Invalid page or pageSize value."); // ❌ Invalid input
        }

        var query = _context.Notes.AsQueryable();

        if (!_currentUser.IsInRole("GlobalAdmin"))
            query = query.Where(n => n.CompanyId == _currentUser.CompanyId);

        var totalCount = await query.CountAsync(); // 🔢 Total

        var items = await query
            .OrderByDescending(n => n.CreatedAt) // ⬇️ Latest first
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(NoteMapper.ToNoteDto())
            .ToListAsync();

        _logger.LogInformation("✅ Retrieved {Count} paged notes.", items.Count);

        var result = new PagedResultDTO<NoteDTO>
        {
            Page = page,
            PageSize = pageSize,
            ImportedCount = totalCount,
            Items = items
        };

        return Ok(result); // ✅ Return result
    }

    #endregion

    #region 📦 Bulk Operations

    /// <summary>
    /// 📦 Creates multiple notes in bulk.
    /// </summary>
    [HttpPost("bulk-create")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> BulkCreateAsync(List<NoteCreateDTO> dtos)
    {
        _logger.LogInformation("📦 Bulk create of {Count} notes started by user {UserId}.", dtos.Count, _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ Bulk create failed – no company assigned to user {UserId}.", _currentUser.UserId);
            return BadRequest("No company assigned to current user.");
        }

        var created = new List<NoteDTO>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                errors.Add(new(index, "Title is required.", "Title", "REQUIRED"));
                _logger.LogWarning("⚠️ Note at index {Index} has missing title.", index);
                continue;
            }

            var entity = new Note
            {
                Title = dto.Title,
                Content = dto.Content,
                ProjectId = dto.ProjectId,
                ScreenId = dto.ScreenId,
                ScreenActionId = dto.ScreenActionId,
                CompanyId = _currentUser.CompanyId.Value,
                UserId = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _context.Notes.Add(entity);
            await _context.SaveChangesAsync();

            var result = await _context.Notes
                .Where(n => n.Id == entity.Id)
                .Select(NoteMapper.ToNoteDto())
                .FirstAsync();

            created.Add(result);
        }

        _logger.LogInformation("✅ Bulk create finished: {CreatedCount} notes created, {ErrorCount} errors.",
            created.Count, errors.Count);

        return Ok(new BulkOperationResultDTO<NoteDTO>
        {
            ImportedCount = created.Count,
            Errors = errors,
            Items = created
        });
    }

    /// <summary>
    /// ✏️ Bulk update for multiple notes.
    /// </summary>
    [HttpPut("bulk-update")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> BulkUpdateAsync(List<NoteUpdateDTO> dtos)
    {
        _logger.LogInformation("✏️ Bulk update of {Count} notes started by user {UserId}.", dtos.Count, _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ Bulk update failed – no company assigned to user {UserId}.", _currentUser.UserId);
            return BadRequest("No company assigned to current user.");
        }

        var updated = new List<NoteDTO>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
        {
            var entity = await _context.Notes.FirstOrDefaultAsync(n =>
                n.Id == dto.Id &&
                (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

            if (entity == null)
            {
                errors.Add(new(index, $"Note with Id {dto.Id} not found or access denied.", "Id", "NOT_FOUND"));
                _logger.LogWarning("❌ Note {Id} not found or not authorized (index {Index}).", dto.Id, index);
                continue;
            }

            entity.Title = dto.Title;
            entity.Content = dto.Content;
            entity.ProjectId = dto.ProjectId;
            entity.ScreenId = dto.ScreenId;
            entity.ScreenActionId = dto.ScreenActionId;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();

            var result = await _context.Notes
                .Where(n => n.Id == entity.Id)
                .Select(NoteMapper.ToNoteDto())
                .FirstAsync();

            updated.Add(result);
        }

        _logger.LogInformation("✅ Bulk update finished: {UpdatedCount} notes updated, {ErrorCount} errors.",
            updated.Count, errors.Count);

        return Ok(new BulkOperationResultDTO<NoteDTO>
        {
            ImportedCount = updated.Count,
            Errors = errors,
            Items = updated
        });
    }

    /// <summary>
    /// 🗑️ Bulk delete (soft) for multiple notes.
    /// </summary>
    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> BulkDeleteAsync(List<long> ids)
    {
        _logger.LogInformation("🗑️ Bulk delete for {Count} notes started by user {UserId}.", ids.Count, _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ Bulk delete failed – no company assigned to user {UserId}.", _currentUser.UserId);
            return BadRequest("No company assigned to current user.");
        }

        var deleted = new List<NoteDTO>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (id, index) in ids.Select((x, i) => (x, i)))
        {
            var entity = await _context.Notes.FirstOrDefaultAsync(n =>
                n.Id == id &&
                (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

            if (entity == null)
            {
                errors.Add(new(index, $"Note with Id {id} not found or access denied.", "Id", "NOT_FOUND"));
                _logger.LogWarning("❌ Note {Id} not found or not authorized (index {Index}).", id, index);
                continue;
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();

            var result = await _context.Notes
                .IgnoreQueryFilters()
                .Where(n => n.Id == entity.Id)
                .Select(NoteMapper.ToNoteDto())
                .FirstAsync();

            deleted.Add(result);
        }

        _logger.LogInformation("✅ Bulk delete finished: {DeletedCount} notes deleted, {ErrorCount} errors.",
            deleted.Count, errors.Count);

        return Ok(new BulkOperationResultDTO<NoteDTO>
        {
            ImportedCount = deleted.Count,
            Errors = errors,
            Items = deleted
        });
    }

    #endregion

    #region 📥 Import / 📤 Export

    /// <summary>
    /// 📥 Imports notes from a CSV file.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> ImportNotes(IFormFile file)
    {
        _logger.LogInformation("📥 Importing notes from uploaded CSV file by user {UserId}.", _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ Import failed – no company assigned to user {UserId}.", _currentUser.UserId);
            return BadRequest("No company assigned to current user.");
        }

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("❌ Import failed – no file uploaded.");
            return BadRequest("No file uploaded.");
        }

        var result = new BulkOperationResultDTO<NoteDTO>();

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<NoteImportMap>();

        var records = csv.GetRecords<NoteImportDTO>().ToList();

        foreach (var (record, index) in records.Select((r, i) => (r, i)))
        {
            if (string.IsNullOrWhiteSpace(record.Title))
            {
                result.Errors.Add(new(index, "Title is required.", "Title", "REQUIRED"));
                _logger.LogWarning("⚠️ CSV row {Index} has missing title.", index);
                continue;
            }

            var entity = new Note
            {
                Title = record.Title,
                Content = record.Content,
                ProjectId = record.ProjectId,
                ScreenId = record.ScreenId,
                ScreenActionId = record.ScreenActionId,
                CompanyId = _currentUser.CompanyId.Value,
                UserId = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _context.Notes.Add(entity);
            await _context.SaveChangesAsync();

            result.ImportedCount++;
        }

        result.TotalRows = records.Count;

        _logger.LogInformation("✅ Import completed: {ImportedCount} notes imported, {ErrorCount} errors.",
            result.ImportedCount, result.Errors.Count);

        return Ok(result);
    }

    /// <summary>
    /// 📤 Exports all notes to CSV format.
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> ExportNotesAsCsv()
    {
        _logger.LogInformation("📤 Exporting notes to CSV for user {UserId}.", _currentUser.UserId);

        var query = _context.Notes.AsQueryable();

        if (!_currentUser.IsInRole("GlobalAdmin"))
            query = query.Where(n => n.CompanyId == _currentUser.CompanyId);

        var items = await query
            .OrderBy(n => n.CreatedAt)
            .Select(NoteMapper.ToNoteDto())
            .ToListAsync();

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(items);

        var bytes = Encoding.UTF8.GetBytes(writer.ToString());

        _logger.LogInformation("✅ Export completed: {Count} notes written to CSV.", items.Count);

        return File(bytes, "text/csv", "notes_export.csv");
    }

    #endregion
}



///// @file NoteController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-04
///// @brief API controller for managing notes including CRUD, bulk ops, CSV import/export, and soft delete.
///// @details
///// This controller provides endpoints for creating, reading, updating, deleting, restoring,
///// and importing/exporting note entries. It supports multi-tenancy, authorization, and
///// bulk operations for Admins and GlobalAdmins.

//using CsvHelper;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Globalization;
//using System.Text;
//using UserFlow.API.Data;
//using UserFlow.API.Data.Entities;
//using UserFlow.API.Mappers;
//using UserFlow.API.Services.Interfaces;
//using UserFlow.API.Shared.DTO;
//using UserFlow.API.Shared.DTO.ImportMaps;

//namespace UserFlow.API.Controllers;

///// <summary>
///// 📎 API controller for managing notes.
///// </summary>
//[ApiController] // ✅ Enables model binding, validation, etc.
//[Route("api/notes")] // 📍 Sets base route
//[Authorize] // 🔐 Only authenticated users can access
//public class NoteController : ControllerBase
//{
//    #region 🔒 Fields

//    /// <summary>
//    /// 🗃️ EF database context
//    /// </summary>
//    private readonly AppDbContext _db;

//    /// <summary>
//    /// 👤 Service for accessing the current user context
//    /// </summary>
//    private readonly ICurrentUserService _currentUser;

//    #endregion

//    #region 🔧 Constructor

//    /// <summary>
//    /// 🛠️ Constructor injecting dependencies
//    /// </summary>
//    public NoteController(AppDbContext db, ICurrentUserService currentUser)
//    {
//        _db = db; // 🧱 Assign database context
//        _currentUser = currentUser; // 🧱 Assign user context
//    }

//    #endregion

//    #region 📄 CRUD

//    /// <summary>
//    /// 📄 Returns all notes for the current company (unless GlobalAdmin).
//    /// </summary>
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<NoteDTO>>> GetNotes()
//    {
//        var query = _db.Notes.AsQueryable(); // 🔍 Start query

//        if (!_currentUser.IsInRole("GlobalAdmin")) // 🔐 Restrict by company
//            query = query.Where(n => n.CompanyId == _currentUser.CompanyId);

//        var result = await query
//            .OrderByDescending(n => n.CreatedAt) // ⬇️ Latest first
//            .Select(NoteMapper.ToNoteDto()) // 🔁 Project to DTO
//            .ToListAsync();

//        return Ok(result); // ✅ Return result
//    }

//    /// <summary>
//    /// 🔎 Retrieves a note by ID.
//    /// </summary>
//    [HttpGet("{id:long}")]
//    public async Task<ActionResult<NoteDTO>> GetNoteById(long id)
//    {
//        var result = await _db.Notes
//            .Where(n => n.Id == id)
//            .Where(n => _currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId)
//            .Select(NoteMapper.ToNoteDto())
//            .FirstOrDefaultAsync(); // 🔍 Single result

//        if (result == null)
//            return NotFound(); // ❌ Not found

//        return Ok(result); // ✅ Return DTO
//    }

//    /// <summary>
//    /// ➕ Creates a new note.
//    /// </summary>
//    [HttpPost]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<NoteDTO>> CreateNote(NoteCreateDTO dto)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Validation failed

//        var entity = new Note
//        {
//            Title = dto.Title,
//            Content = dto.Content,
//            ProjectId = dto.ProjectId,
//            ScreenId = dto.ScreenId,
//            ScreenActionId = dto.ScreenActionId,
//            CompanyId = _currentUser.CompanyId.Value,
//            UserId = _currentUser.UserId,
//            CreatedAt = DateTime.UtcNow, // 🕒 Timestamp
//            CreatedBy = _currentUser.UserId // 👤 Author
//        };

//        _db.Notes.Add(entity); // 💾 Insert entity
//        await _db.SaveChangesAsync(); // 💾 Save to DB

//        var result = await _db.Notes
//            .Where(n => n.Id == entity.Id)
//            .Select(NoteMapper.ToNoteDto())
//            .FirstAsync();

//        return CreatedAtAction(nameof(GetNoteById), new { id = result.Id }, result); // ✅ Return created
//    }

//    /// <summary>
//    /// ✏️ Updates a note.
//    /// </summary>
//    [HttpPut("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> UpdateNote(long id, NoteUpdateDTO dto)
//    {
//        var entity = await _db.Notes.FirstOrDefaultAsync(n =>
//            n.Id == id && (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

//        if (entity == null)
//            return NotFound(); // ❌ Not found

//        entity.Title = dto.Title;
//        entity.Content = dto.Content;
//        entity.ProjectId = dto.ProjectId;
//        entity.ScreenId = dto.ScreenId;
//        entity.ScreenActionId = dto.ScreenActionId;
//        entity.UpdatedAt = DateTime.UtcNow; // 🕒 Modified
//        entity.UpdatedBy = _currentUser.UserId; // 👤 Modifier

//        await _db.SaveChangesAsync(); // 💾 Save
//        return NoContent(); // ✅ Success
//    }

//    /// <summary>
//    /// 🗑️ Soft-deletes a note.
//    /// </summary>
//    [HttpDelete("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> DeleteNote(long id)
//    {
//        var entity = await _db.Notes.FirstOrDefaultAsync(n =>
//            n.Id == id && (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

//        if (entity == null)
//            return NotFound(); // ❌ Not found

//        entity.IsDeleted = true; // 🗑️ Soft delete
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync(); // 💾 Save
//        return NoContent(); // ✅ Success
//    }

//    /// <summary>
//    /// ♻️ Restores a soft-deleted note.
//    /// </summary>
//    [HttpPost("{id:long}/restore")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> RestoreNote(long id)
//    {
//        var entity = await _db.Notes
//            .IgnoreQueryFilters() // ❗ Bypass soft delete filter
//            .FirstOrDefaultAsync(n =>
//                n.Id == id &&
//                n.IsDeleted &&
//                (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

//        if (entity == null)
//            return NotFound(); // ❌ Not found

//        entity.IsDeleted = false; // ♻️ Restore
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync(); // 💾 Save
//        return NoContent(); // ✅ Done
//    }

//    /// <summary>
//    /// 📄 Returns a paginated list of notes.
//    /// </summary>
//    [HttpGet("paged")]
//    public async Task<ActionResult<PagedResultDTO<NoteDTO>>> GetPagedNotes(int page = 1, int pageSize = 20)
//    {
//        if (page < 1 || pageSize < 1 || pageSize > 200)
//            return BadRequest("Invalid page or pageSize value."); // ❌ Invalid input

//        var query = _db.Notes.AsQueryable();

//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(n => n.CompanyId == _currentUser.CompanyId);

//        var ImportedCount = await query.CountAsync(); // 🔢 Total

//        var items = await query
//            .OrderByDescending(n => n.CreatedAt) // ⬇️ Latest first
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .Select(NoteMapper.ToNoteDto())
//            .ToListAsync();

//        var result = new PagedResultDTO<NoteDTO>
//        {
//            Page = page,
//            PageSize = pageSize,
//            ImportedCount = ImportedCount,
//            Items = items
//        };

//        return Ok(result); // ✅ Return result
//    }

//    #endregion

//    #region 📦 Bulk Operations

//    /// <summary>
//    /// 📦 Creates multiple notes in bulk.
//    /// </summary>
//    [HttpPost("bulk-create")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> BulkCreateNotes(List<NoteCreateDTO> dtos)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Company required

//        var created = new List<NoteDTO>(); // 📋 Created DTOs
//        var errors = new List<BulkOperationErrorDTO>(); // ⚠️ Error list

//        foreach (var (dto, index) in dtos.Select((x, i) => (x, i))) // 🔁 Loop with index
//        {
//            if (string.IsNullOrWhiteSpace(dto.Title)) // ❌ Missing title
//            {
//                errors.Add(new(index, "Title is required.", "Title", "REQUIRED"));
//                continue;
//            }

//            var entity = new Note
//            {
//                Title = dto.Title,
//                Content = dto.Content,
//                ProjectId = dto.ProjectId,
//                ScreenId = dto.ScreenId,
//                ScreenActionId = dto.ScreenActionId,
//                CompanyId = _currentUser.CompanyId.Value,
//                UserId = _currentUser.UserId,
//                CreatedAt = DateTime.UtcNow, // 🕒 Created
//                CreatedBy = _currentUser.UserId // 👤 Creator
//            };

//            _db.Notes.Add(entity); // 💾 Add to context
//            await _db.SaveChangesAsync(); // 💾 Commit

//            var dtoResult = await _db.Notes
//                .Where(n => n.Id == entity.Id)
//                .Select(NoteMapper.ToNoteDto())
//                .FirstAsync(); // 🔄 Project

//            created.Add(dtoResult); // ➕ Add to result
//        }

//        return Ok(new BulkOperationResultDTO<NoteDTO>
//        {
//            ImportedCount = created.Count, // 🔢 Count
//            Errors = errors, // ⚠️ Errors
//            Items = created // 📋 Result
//        });
//    }

//    /// <summary>
//    /// ✏️ Bulk update for multiple notes.
//    /// </summary>
//    [HttpPut("bulk-update")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> BulkUpdateNotes(List<NoteUpdateDTO> dtos)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Company required

//        var updated = new List<NoteDTO>(); // 📋 Updated DTOs
//        var errors = new List<BulkOperationErrorDTO>(); // ⚠️ Error list

//        foreach (var (dto, index) in dtos.Select((x, i) => (x, i))) // 🔁 Loop with index
//        {
//            var entity = await _db.Notes.FirstOrDefaultAsync(n =>
//                n.Id == dto.Id && (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

//            if (entity == null) // ❌ Not found
//            {
//                errors.Add(new(index, $"Note with Id {dto.Id} not found or access denied.", "Id", "NOT_FOUND"));
//                continue;
//            }

//            entity.Title = dto.Title;
//            entity.Content = dto.Content;
//            entity.ProjectId = dto.ProjectId;
//            entity.ScreenId = dto.ScreenId;
//            entity.ScreenActionId = dto.ScreenActionId;
//            entity.UpdatedAt = DateTime.UtcNow; // 🕒 Updated
//            entity.UpdatedBy = _currentUser.UserId; // 👤 Updater

//            await _db.SaveChangesAsync(); // 💾 Commit

//            var dtoResult = await _db.Notes
//                .Where(n => n.Id == entity.Id)
//                .Select(NoteMapper.ToNoteDto())
//                .FirstAsync(); // 🔄 Project

//            updated.Add(dtoResult); // ➕ Add to result
//        }

//        return Ok(new BulkOperationResultDTO<NoteDTO>
//        {
//            ImportedCount = updated.Count,
//            Errors = errors,
//            Items = updated
//        });
//    }

//    /// <summary>
//    /// 🗑️ Bulk delete (soft) for multiple notes.
//    /// </summary>
//    [HttpPost("bulk-delete")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> BulkDeleteNotes(List<long> ids)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Company required

//        var deleted = new List<NoteDTO>(); // 📋 Deleted notes
//        var errors = new List<BulkOperationErrorDTO>(); // ⚠️ Error list

//        foreach (var (id, index) in ids.Select((x, i) => (x, i))) // 🔁 Loop with index
//        {
//            var entity = await _db.Notes.FirstOrDefaultAsync(n =>
//                n.Id == id && (_currentUser.IsInRole("GlobalAdmin") || n.CompanyId == _currentUser.CompanyId));

//            if (entity == null) // ❌ Not found
//            {
//                errors.Add(new(index, $"Note with Id {id} not found or access denied.", "Id", "NOT_FOUND"));
//                continue;
//            }

//            entity.IsDeleted = true; // 🗑️ Soft delete
//            entity.UpdatedAt = DateTime.UtcNow;
//            entity.UpdatedBy = _currentUser.UserId;

//            await _db.SaveChangesAsync(); // 💾 Commit

//            var dtoResult = await _db.Notes
//                .IgnoreQueryFilters()
//                .Where(n => n.Id == entity.Id)
//                .Select(NoteMapper.ToNoteDto())
//                .FirstAsync(); // 🔄 Project

//            deleted.Add(dtoResult); // ➕ Add to result
//        }

//        return Ok(new BulkOperationResultDTO<NoteDTO>
//        {
//            ImportedCount = deleted.Count,
//            Errors = errors,
//            Items = deleted
//        });
//    }

//    #endregion

//    #region 📥 Import / 📤 Export

//    /// <summary>
//    /// 📥 Imports notes from a CSV file.
//    /// </summary>
//    [HttpPost("import")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<NoteDTO>>> ImportNotes(IFormFile file)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Company required

//        if (file == null || file.Length == 0)
//            return BadRequest("No file uploaded."); // ❌ Missing file

//        var result = new BulkOperationResultDTO<NoteDTO>();

//        using var reader = new StreamReader(file.OpenReadStream());
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
//        csv.Context.RegisterClassMap<NoteImportMap>(); // 📄 Map CSV columns

//        var records = csv.GetRecords<NoteImportDTO>().ToList(); // 📄 Read all

//        foreach (var (record, index) in records.Select((r, i) => (r, i)))
//        {
//            if (string.IsNullOrWhiteSpace(record.Title))
//            {
//                result.Errors.Add(new BulkOperationErrorDTO(index, "Title is required.", "Title", "REQUIRED"));
//                continue; // ⚠️ Skip invalid
//            }

//            var entity = new Note
//            {
//                Title = record.Title,
//                Content = record.Content,
//                ProjectId = record.ProjectId,
//                ScreenId = record.ScreenId,
//                ScreenActionId = record.ScreenActionId,
//                CompanyId = _currentUser.CompanyId.Value,
//                UserId = _currentUser.UserId,
//                CreatedAt = DateTime.UtcNow, // 🕒 Timestamp
//                CreatedBy = _currentUser.UserId // 👤 Creator
//            };

//            _db.Notes.Add(entity); // 💾 Insert
//            await _db.SaveChangesAsync(); // 💾 Commit

//            result.ImportedCount++; // ➕ Count
//        }

//        result.TotalRows = records.Count; // 🔢 Total processed

//        return Ok(result); // ✅ Done
//    }

//    /// <summary>
//    /// 📤 Exports all notes to CSV format.
//    /// </summary>
//    [HttpGet("export")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> ExportNotesAsCsv()
//    {
//        var query = _db.Notes.AsQueryable();

//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(n => n.CompanyId == _currentUser.CompanyId);

//        var items = await query
//            .OrderBy(n => n.CreatedAt) // 📅 Sort by date
//            .Select(NoteMapper.ToNoteDto())
//            .ToListAsync();

//        using var writer = new StringWriter(); // 📝 Output writer
//        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

//        csv.WriteRecords(items); // 📤 Write CSV

//        var bytes = Encoding.UTF8.GetBytes(writer.ToString());

//        return File(bytes, "text/csv", "notes_export.csv"); // 📎 Download
//    }

//    #endregion

//}
