/// @file ScreenController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-05
/// @brief Controller to manage Screens (CRUD, Bulk, Import/Export, Restore, Pagination)
/// @details This controller handles the full lifecycle of Screen entities with role-based authorization, soft delete, bulk operations and CSV support.
/// @endpoints
/// - GET    /api/screens                  → Get all screens (with company/role filter)
/// - GET    /api/screens/{id}            → Get single screen by ID
/// - POST   /api/screens                 → Create screen (Admin/GlobalAdmin)
/// - PUT    /api/screens/{id}           → Update screen (Admin/GlobalAdmin)
/// - DELETE /api/screens/{id}           → Soft delete screen (Admin/GlobalAdmin)
/// - POST   /api/screens/{id}/restore   → Restore deleted screen (Admin/GlobalAdmin)
/// - GET    /api/screens/paged          → Paginated list of screens
/// - POST   /api/screens/bulk-create    → Bulk create screens
/// - PUT    /api/screens/bulk-update    → Bulk update screens
/// - POST   /api/screens/bulk-delete    → Bulk soft delete screens
/// - GET    /api/screens/export         → Export all screens as CSV
/// - POST   /api/screens/import         → Import screens from uploaded CSV

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

#region 🔐 Authorization & Routing

/// 🔐 Require authorization for all endpoints in this controller
[ApiController]
[Route("api/screens")]
[Authorize]
public class ScreenController : ControllerBase

#endregion
{
    #region 🔒 Fields

    /// 💾 Database context for EF Core operations
    private readonly AppDbContext _db;

    /// 👤 Service to access current user's identity and roles
    private readonly ICurrentUserService _currentUser;

    /// 📝 Logger for tracking and debugging
    private readonly ILogger<ScreenController> _logger;

    #endregion

    #region 🔧 Constructor

    /// 🛠 Constructor to inject dependencies
    public ScreenController(AppDbContext db, ICurrentUserService currentUser, ILogger<ScreenController> logger)
    {
        _db = db;                             // 💾 Store injected DbContext
        _currentUser = currentUser;           // 👤 Store injected user context
        _logger = logger;                     // 📝 Store injected logger
    }

    #endregion

    #region 📄 CRUD – Create, Read, Update, Delete

    /// <summary>
    /// 📥 Get all Screens (filtered by company unless GlobalAdmin)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScreenDTO>>> GetScreens()
    {
        _logger.LogInformation("📥 Retrieving all screens for user {UserId}", _currentUser.UserId);

        var query = _db.Screens.AsQueryable(); // 🧮 Start with all Screens

        if (!_currentUser.IsInRole("GlobalAdmin"))
        {
            query = query.Where(s => s.CompanyId == _currentUser.CompanyId); // 🔐 Restrict to own company if not GlobalAdmin
            _logger.LogInformation("🔐 Applied company filter for CompanyId={CompanyId}", _currentUser.CompanyId);
        }

        var result = await query
            .OrderByDescending(s => s.CreatedAt)            // 🕒 Order by newest first
            .Select(ScreenMapper.ToScreenDto())             // 🧠 Map to DTO projection
            .ToListAsync();                                 // 🚀 Execute query

        _logger.LogInformation("✅ Retrieved {Count} screens", result.Count);
        return Ok(result); // ✅ Return result as 200 OK
    }

    /// <summary>
    /// 📥 Get a single screen by ID (only if authorized)
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ScreenDTO>> GetScreenById(long id)
    {
        _logger.LogInformation("🔍 Retrieving screen with ID {Id}", id);

        var result = await _db.Screens
            .Where(s => s.Id == id) // 🔍 Match by ID
            .Where(s => _currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId) // 🔐 Company check
            .Select(ScreenMapper.ToScreenDto()) // 🧠 Map to DTO
            .FirstOrDefaultAsync(); // 📦 Return first result or null

        if (result == null)
        {
            _logger.LogWarning("❌ Screen with ID {Id} not found or unauthorized", id);
            return NotFound(); // ❌ 404 if not found or not authorized
        }

        return Ok(result); // ✅ Return as 200 OK
    }

    /// <summary>
    /// ➕ Create a new Screen (Admin or GlobalAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<ScreenDTO>> CreateScreen(ScreenCreateDTO dto)
    {
        _logger.LogInformation("➕ Creating new screen with name '{Name}'", dto.Name);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ Cannot create screen – user has no assigned company.");
            return BadRequest("No company assigned to current user."); // ❌ Must belong to a company
        }

        /// 🆕 Create new entity from DTO
        var entity = new Screen
        {
            Name = dto.Name,
            Identifier = dto.Identifier,
            Description = dto.Description,
            Type = dto.Type,
            ProjectId = dto.ProjectId,
            CompanyId = _currentUser.CompanyId.Value,
            UserId = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _db.Screens.Add(entity);       // 💾 Add to context
        await _db.SaveChangesAsync();  // 💾 Commit changes

        _logger.LogInformation("✅ Screen created with ID {Id}", entity.Id);

        /// 📦 Reload with DTO projection
        var result = await _db.Screens
            .Where(s => s.Id == entity.Id)
            .Select(ScreenMapper.ToScreenDto())
            .FirstAsync();

        return CreatedAtAction(nameof(GetScreenById), new { id = result.Id }, result); // ✅ Return 201 Created
    }

    /// <summary>
    /// ✏️ Update existing Screen
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> UpdateScreen(long id, ScreenUpdateDTO dto)
    {
        _logger.LogInformation("✏️ Updating screen with ID {Id}", id);

        var entity = await _db.Screens
            .FirstOrDefaultAsync(s => s.Id == id &&
                (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Screen with ID {Id} not found or unauthorized", id);
            return NotFound();
        }

        entity.Name = dto.Name;
        entity.Identifier = dto.Identifier;
        entity.Description = dto.Description;
        entity.Type = dto.Type;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();
        _logger.LogInformation("✅ Screen {Id} updated successfully", id);

        return NoContent();
    }

    /// <summary>
    /// 🗑 Soft delete a screen
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> DeleteScreen(long id)
    {
        _logger.LogInformation("🗑 Deleting screen with ID {Id}", id);

        var entity = await _db.Screens
            .FirstOrDefaultAsync(s => s.Id == id &&
                (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Screen {Id} not found or access denied", id);
            return NotFound();
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Screen {Id} marked as deleted", id);
        return NoContent();
    }

    /// <summary>
    /// 🔁 Restore a soft-deleted screen
    /// </summary>
    [HttpPost("{id:long}/restore")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> RestoreScreen(long id)
    {
        _logger.LogInformation("🔁 Attempting to restore screen {Id}", id);

        var entity = await _db.Screens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.IsDeleted &&
                (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Cannot restore screen {Id} – not found or not deleted", id);
            return NotFound();
        }

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();
        _logger.LogInformation("✅ Screen {Id} restored", id);

        return NoContent();
    }

    /// <summary>
    /// 📄 Get screens with pagination
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDTO<ScreenDTO>>> GetPagedScreens(int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("📄 Retrieving paged screens: page {Page}, size {Size}", page, pageSize);

        if (page < 1 || pageSize < 1 || pageSize > 200)
        {
            _logger.LogWarning("❌ Invalid paging parameters: page={Page}, size={Size}", page, pageSize);
            return BadRequest("Invalid page or pageSize value.");
        }

        var query = _db.Screens.AsQueryable();

        if (!_currentUser.IsInRole("GlobalAdmin"))
        {
            query = query.Where(s => s.CompanyId == _currentUser.CompanyId);
            _logger.LogInformation("🔐 Applied company filter for paging");
        }

        var ImportedCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ScreenMapper.ToScreenDto())
            .ToListAsync();

        var result = new PagedResultDTO<ScreenDTO>
        {
            Page = page,
            PageSize = pageSize,
            ImportedCount = ImportedCount,
            Items = items
        };

        _logger.LogInformation("✅ Returning {Count} screens", items.Count);
        return Ok(result);
    }

    #endregion

    #region 📦 Bulk – Create, Update, Delete Multiple Screens

    /// <summary>
    /// 📥 Bulk create multiple screens
    /// </summary>
    [HttpPost("bulk-create")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> BulkCreateScreens(List<ScreenCreateDTO> dtos)
    {
        _logger.LogInformation("📥 Starting bulk screen creation ({Count} items)", dtos.Count);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ User has no company assigned");
            return BadRequest("No company assigned to current user.");
        }

        var created = new List<ScreenDTO>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                errors.Add(new(index, "Screen name is required.", "Name", "REQUIRED"));
                _logger.LogWarning("⚠️ Skipped index {Index}: Name is empty", index);
                continue;
            }

            var entity = new Screen
            {
                Name = dto.Name,
                Identifier = dto.Identifier,
                Description = dto.Description,
                Type = dto.Type,
                ProjectId = dto.ProjectId,
                CompanyId = _currentUser.CompanyId.Value,
                UserId = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _db.Screens.Add(entity);
            await _db.SaveChangesAsync();

            var dtoResult = await _db.Screens
                .Where(s => s.Id == entity.Id)
                .Select(ScreenMapper.ToScreenDto())
                .FirstAsync();

            created.Add(dtoResult);
        }

        _logger.LogInformation("✅ Bulk create finished: {Success} created, {Failed} failed", created.Count, errors.Count);

        return Ok(new BulkOperationResultDTO<ScreenDTO>
        {
            ImportedCount = created.Count,
            Errors = errors,
            Items = created
        });
    }

    /// <summary>
    /// ✏️ Bulk update of screens
    /// </summary>
    [HttpPut("bulk-update")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> BulkUpdateScreens(List<ScreenUpdateDTO> dtos)
    {
        _logger.LogInformation("✏️ Starting bulk screen update ({Count} items)", dtos.Count);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ User has no company assigned");
            return BadRequest("No company assigned to current user.");
        }

        var updated = new List<ScreenDTO>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
        {
            var entity = await _db.Screens.FirstOrDefaultAsync(s =>
                s.Id == dto.Id && (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

            if (entity == null)
            {
                errors.Add(new(index, $"Screen with Id {dto.Id} not found or access denied.", "Id", "NOT_FOUND"));
                _logger.LogWarning("⚠️ Skipped index {Index}: Screen not found", index);
                continue;
            }

            entity.Name = dto.Name;
            entity.Identifier = dto.Identifier;
            entity.Description = dto.Description;
            entity.Type = dto.Type;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync();

            var dtoResult = await _db.Screens
                .Where(s => s.Id == entity.Id)
                .Select(ScreenMapper.ToScreenDto())
                .FirstAsync();

            updated.Add(dtoResult);
        }

        _logger.LogInformation("✅ Bulk update finished: {Success} updated, {Failed} failed", updated.Count, errors.Count);

        return Ok(new BulkOperationResultDTO<ScreenDTO>
        {
            ImportedCount = updated.Count,
            Errors = errors,
            Items = updated
        });
    }

    /// <summary>
    /// 🗑 Bulk soft delete of screens
    /// </summary>
    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> BulkDeleteScreens(List<long> ids)
    {
        _logger.LogInformation("🗑 Starting bulk screen deletion ({Count} IDs)", ids.Count);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ User has no company assigned");
            return BadRequest("No company assigned to current user.");
        }

        var deleted = new List<ScreenDTO>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (id, index) in ids.Select((x, i) => (x, i)))
        {
            var entity = await _db.Screens.FirstOrDefaultAsync(s =>
                s.Id == id && (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

            if (entity == null)
            {
                errors.Add(new(index, $"Screen with Id {id} not found or access denied.", "Id", "NOT_FOUND"));
                _logger.LogWarning("⚠️ Skipped index {Index}: Screen not found", index);
                continue;
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync();

            var dtoResult = await _db.Screens
                .IgnoreQueryFilters()
                .Where(s => s.Id == entity.Id)
                .Select(ScreenMapper.ToScreenDto())
                .FirstAsync();

            deleted.Add(dtoResult);
        }

        _logger.LogInformation("✅ Bulk delete finished: {Success} deleted, {Failed} failed", deleted.Count, errors.Count);

        return Ok(new BulkOperationResultDTO<ScreenDTO>
        {
            ImportedCount = deleted.Count,
            Errors = errors,
            Items = deleted
        });
    }

    #endregion

    #region 📥 Import / 📤 Export CSV

    /// <summary>
    /// 📤 Export all screens to CSV
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> ExportScreensAsCsv()
    {
        _logger.LogInformation("📤 Exporting screens to CSV by user {UserId}", _currentUser.UserId);

        var query = _db.Screens.AsQueryable();

        if (!_currentUser.IsInRole("GlobalAdmin"))
        {
            query = query.Where(s => s.CompanyId == _currentUser.CompanyId);
            _logger.LogInformation("🔐 Filtering export to company {CompanyId}", _currentUser.CompanyId);
        }

        var items = await query
            .OrderBy(s => s.Name)
            .Select(ScreenMapper.ToScreenDto())
            .ToListAsync();

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(items);

        var bytes = Encoding.UTF8.GetBytes(writer.ToString());

        _logger.LogInformation("✅ Exported {Count} screens to CSV", items.Count);

        return File(bytes, "text/csv", "screens_export.csv");
    }

    /// <summary>
    /// 📥 Import screens from uploaded CSV file
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> ImportScreens(IFormFile file)
    {
        _logger.LogInformation("📥 Importing screens from CSV by user {UserId}", _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ User has no company assigned");
            return BadRequest("No company assigned to current user.");
        }

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("❌ No CSV file uploaded");
            return BadRequest("No file uploaded.");
        }

        var result = new BulkOperationResultDTO<ScreenDTO>();

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ScreenImportMap>();

        var records = csv.GetRecords<ScreenImportDTO>().ToList();
        _logger.LogInformation("📊 Parsed {Count} rows from CSV", records.Count);

        foreach (var (record, index) in records.Select((r, i) => (r, i)))
        {
            if (string.IsNullOrWhiteSpace(record.Name))
            {
                result.Errors.Add(new BulkOperationErrorDTO(index, "Screen name is required.", "Name", "REQUIRED"));
                _logger.LogWarning("⚠️ Skipped row {Index}: Name is required", index);
                continue;
            }

            var entity = new Screen
            {
                Name = record.Name,
                Identifier = record.Identifier,
                Description = record.Description,
                Type = record.Type,
                ProjectId = record.ProjectId,
                CompanyId = _currentUser.CompanyId.Value,
                UserId = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _db.Screens.Add(entity);
            await _db.SaveChangesAsync();
            result.ImportedCount++;
        }

        result.TotalRows = records.Count;

        _logger.LogInformation("✅ Finished screen import: {Success} imported, {Failed} failed",
            result.ImportedCount, result.Errors.Count);

        return Ok(result);
    }

    #endregion
}




///// @file ScreenController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-05
///// @brief Controller to manage Screens (CRUD, Bulk, Import/Export, Restore, Pagination)
///// @details This controller handles the full lifecycle of Screen entities with role-based authorization, soft delete, bulk operations and CSV support.
///// @endpoints
///// - GET    /api/screens                  → Get all screens (with company/role filter)
///// - GET    /api/screens/{id}            → Get single screen by ID
///// - POST   /api/screens                 → Create screen (Admin/GlobalAdmin)
///// - PUT    /api/screens/{id}           → Update screen (Admin/GlobalAdmin)
///// - DELETE /api/screens/{id}           → Soft delete screen (Admin/GlobalAdmin)
///// - POST   /api/screens/{id}/restore   → Restore deleted screen (Admin/GlobalAdmin)
///// - GET    /api/screens/paged          → Paginated list of screens
///// - POST   /api/screens/bulk-create    → Bulk create screens
///// - PUT    /api/screens/bulk-update    → Bulk update screens
///// - POST   /api/screens/bulk-delete    → Bulk soft delete screens
///// - GET    /api/screens/export         → Export all screens as CSV
///// - POST   /api/screens/import         → Import screens from uploaded CSV

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

//#region 🔐 Authorization & Routing

///// 🔐 Require authorization for all endpoints in this controller
//[ApiController]
//[Route("api/screens")]
//[Authorize]
//public class ScreenController : ControllerBase

//#endregion
//{
//    #region 🔒 Fields

//    /// 💾 Database context for EF Core operations
//    private readonly AppDbContext _db;

//    /// 👤 Service to access current user's identity and roles
//    private readonly ICurrentUserService _currentUser;

//    #endregion

//    #region 🔧 Constructor

//    /// 🛠 Constructor to inject dependencies
//    public ScreenController(AppDbContext db, ICurrentUserService currentUser)
//    {
//        _db = db;                             // 💾 Store injected DbContext
//        _currentUser = currentUser;           // 👤 Store injected user context
//    }

//    #endregion

//    #region 📄 CRUD – Create, Read, Update, Delete

//    /// 📥 Get all Screens (filtered by company unless GlobalAdmin)
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<ScreenDTO>>> GetScreens()
//    {
//        var query = _db.Screens.AsQueryable(); // 🧮 Start with all Screens

//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(s => s.CompanyId == _currentUser.CompanyId); // 🔐 Restrict to own company if not GlobalAdmin

//        var result = await query
//            .OrderByDescending(s => s.CreatedAt)            // 🕒 Order by newest first
//            .Select(ScreenMapper.ToScreenDto())             // 🧠 Map to DTO projection
//            .ToListAsync();                                 // 🚀 Execute query

//        return Ok(result); // ✅ Return result as 200 OK
//    }

//    /// 📥 Get a single screen by ID (only if authorized)
//    [HttpGet("{id:long}")]
//    public async Task<ActionResult<ScreenDTO>> GetScreenById(long id)
//    {
//        var result = await _db.Screens
//            .Where(s => s.Id == id) // 🔍 Match by ID
//            .Where(s => _currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId) // 🔐 Company check
//            .Select(ScreenMapper.ToScreenDto()) // 🧠 Map to DTO
//            .FirstOrDefaultAsync(); // 📦 Return first result or null

//        if (result == null)
//            return NotFound(); // ❌ 404 if not found or not authorized

//        return Ok(result); // ✅ Return as 200 OK
//    }

//    /// ➕ Create a new Screen (Admin or GlobalAdmin only)
//    [HttpPost]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<ScreenDTO>> CreateScreen(ScreenCreateDTO dto)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Must belong to a company

//        /// 🆕 Create new entity from DTO
//        var entity = new Screen
//        {
//            Name = dto.Name,
//            Identifier = dto.Identifier,
//            Description = dto.Description,
//            Type = dto.Type,
//            ProjectId = dto.ProjectId,
//            CompanyId = _currentUser.CompanyId.Value,
//            UserId = _currentUser.UserId,
//            CreatedAt = DateTime.UtcNow,
//            CreatedBy = _currentUser.UserId
//        };

//        _db.Screens.Add(entity);       // 💾 Add to context
//        await _db.SaveChangesAsync();  // 💾 Commit changes

//        /// 📦 Reload with DTO projection
//        var result = await _db.Screens
//            .Where(s => s.Id == entity.Id)
//            .Select(ScreenMapper.ToScreenDto())
//            .FirstAsync();

//        return CreatedAtAction(nameof(GetScreenById), new { id = result.Id }, result); // ✅ Return 201 Created
//    }

//    /// ✏️ Update existing Screen
//    [HttpPut("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> UpdateScreen(long id, ScreenUpdateDTO dto)
//    {
//        var entity = await _db.Screens
//            .FirstOrDefaultAsync(s => s.Id == id &&
//                (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId)); // 🔐 Only own company

//        if (entity == null)
//            return NotFound(); // ❌ Not found or access denied

//        /// ✏️ Update fields
//        entity.Name = dto.Name;
//        entity.Identifier = dto.Identifier;
//        entity.Description = dto.Description;
//        entity.Type = dto.Type;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync(); // 💾 Save changes
//        return NoContent(); // ✅ 204 No Content
//    }

//    /// 🗑 Soft delete a screen
//    [HttpDelete("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> DeleteScreen(long id)
//    {
//        var entity = await _db.Screens
//            .FirstOrDefaultAsync(s => s.Id == id &&
//                (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId)); // 🔐 Company check

//        if (entity == null)
//            return NotFound(); // ❌ Not found or not allowed

//        entity.IsDeleted = true;              // 🗑 Mark as deleted
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();         // 💾 Save changes
//        return NoContent();                   // ✅ 204 No Content
//    }

//    /// 🔁 Restore a soft-deleted screen
//    [HttpPost("{id:long}/restore")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> RestoreScreen(long id)
//    {
//        var entity = await _db.Screens
//            .IgnoreQueryFilters()            // ⚠️ Include soft-deleted
//            .FirstOrDefaultAsync(s =>
//                s.Id == id &&
//                s.IsDeleted &&
//                (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

//        if (entity == null)
//            return NotFound(); // ❌ Not found or not allowed

//        entity.IsDeleted = false;           // ✅ Restore
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();       // 💾 Commit
//        return NoContent();                 // ✅ Success
//    }

//    /// 📄 Get screens with pagination
//    [HttpGet("paged")]
//    public async Task<ActionResult<PagedResultDTO<ScreenDTO>>> GetPagedScreens(int page = 1, int pageSize = 20)
//    {
//        /// ⚠️ Validate parameters
//        if (page < 1 || pageSize < 1 || pageSize > 200)
//            return BadRequest("Invalid page or pageSize value.");

//        var query = _db.Screens.AsQueryable(); // 🧮 Start query

//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(s => s.CompanyId == _currentUser.CompanyId); // 🔐 Filter by company

//        var ImportedCount = await query.CountAsync(); // 🔢 Total count

//        var items = await query
//            .OrderByDescending(s => s.CreatedAt)         // 🕒 Newest first
//            .Skip((page - 1) * pageSize)                 // ⏭ Skip pages
//            .Take(pageSize)                              // 🎯 Take items
//            .Select(ScreenMapper.ToScreenDto())          // 🧠 Project to DTO
//            .ToListAsync();                              // 🚀 Execute

//        var result = new PagedResultDTO<ScreenDTO>
//        {
//            Page = page,
//            PageSize = pageSize,
//            ImportedCount = ImportedCount,
//            Items = items
//        };

//        return Ok(result); // ✅ Return paged result
//    }

//    #endregion

//    #region 📦 Bulk – Create, Update, Delete Multiple Screens

//    /// 📥 Bulk create multiple screens
//    [HttpPost("bulk-create")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> BulkCreateScreens(List<ScreenCreateDTO> dtos)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ User must belong to a company

//        var created = new List<ScreenDTO>();                   // 📦 List of created DTOs
//        var errors = new List<BulkOperationErrorDTO>();        // ⚠️ List of validation errors

//        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
//        {
//            if (string.IsNullOrWhiteSpace(dto.Name))
//            {
//                errors.Add(new(index, "Screen name is required.", "Name", "REQUIRED")); // ❌ Validation error
//                continue;
//            }

//            var entity = new Screen
//            {
//                Name = dto.Name,
//                Identifier = dto.Identifier,
//                Description = dto.Description,
//                Type = dto.Type,
//                ProjectId = dto.ProjectId,
//                CompanyId = _currentUser.CompanyId.Value,
//                UserId = _currentUser.UserId,
//                CreatedAt = DateTime.UtcNow,
//                CreatedBy = _currentUser.UserId
//            };

//            _db.Screens.Add(entity);               // 💾 Add to DbContext
//            await _db.SaveChangesAsync();          // 💾 Commit transaction

//            var dtoResult = await _db.Screens
//                .Where(s => s.Id == entity.Id)
//                .Select(ScreenMapper.ToScreenDto()) // 🧠 Map to DTO
//                .FirstAsync();

//            created.Add(dtoResult);                // ✅ Add to result list
//        }

//        return Ok(new BulkOperationResultDTO<ScreenDTO>
//        {
//            ImportedCount = created.Count,
//            Errors = errors,
//            Items = created
//        });
//    }

//    /// ✏️ Bulk update of screens
//    [HttpPut("bulk-update")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> BulkUpdateScreens(List<ScreenUpdateDTO> dtos)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Company check

//        var updated = new List<ScreenDTO>();                  // 📦 List of updated screens
//        var errors = new List<BulkOperationErrorDTO>();       // ⚠️ Validation error list

//        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
//        {
//            var entity = await _db.Screens.FirstOrDefaultAsync(s =>
//                s.Id == dto.Id && (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

//            if (entity == null)
//            {
//                errors.Add(new(index, $"Screen with Id {dto.Id} not found or access denied.", "Id", "NOT_FOUND")); // ❌ Error
//                continue;
//            }

//            entity.Name = dto.Name;
//            entity.Identifier = dto.Identifier;
//            entity.Description = dto.Description;
//            entity.Type = dto.Type;
//            entity.UpdatedAt = DateTime.UtcNow;
//            entity.UpdatedBy = _currentUser.UserId;

//            await _db.SaveChangesAsync();          // 💾 Commit update

//            var dtoResult = await _db.Screens
//                .Where(s => s.Id == entity.Id)
//                .Select(ScreenMapper.ToScreenDto())
//                .FirstAsync();

//            updated.Add(dtoResult);                // ✅ Add to result
//        }

//        return Ok(new BulkOperationResultDTO<ScreenDTO>
//        {
//            ImportedCount = updated.Count,
//            Errors = errors,
//            Items = updated
//        });
//    }

//    /// 🗑 Bulk soft delete of screens
//    [HttpPost("bulk-delete")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> BulkDeleteScreens(List<long> ids)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Must belong to company

//        var deleted = new List<ScreenDTO>();                // 📦 Successfully deleted
//        var errors = new List<BulkOperationErrorDTO>();     // ⚠️ Validation errors

//        foreach (var (id, index) in ids.Select((x, i) => (x, i)))
//        {
//            var entity = await _db.Screens.FirstOrDefaultAsync(s =>
//                s.Id == id && (_currentUser.IsInRole("GlobalAdmin") || s.CompanyId == _currentUser.CompanyId));

//            if (entity == null)
//            {
//                errors.Add(new(index, $"Screen with Id {id} not found or access denied.", "Id", "NOT_FOUND"));
//                continue;
//            }

//            entity.IsDeleted = true;            // 🗑 Soft delete
//            entity.UpdatedAt = DateTime.UtcNow;
//            entity.UpdatedBy = _currentUser.UserId;

//            await _db.SaveChangesAsync();       // 💾 Save deletion

//            var dtoResult = await _db.Screens
//                .IgnoreQueryFilters()           // ⚠️ Needed to access deleted entities
//                .Where(s => s.Id == entity.Id)
//                .Select(ScreenMapper.ToScreenDto())
//                .FirstAsync();

//            deleted.Add(dtoResult);             // ✅ Add to result
//        }

//        return Ok(new BulkOperationResultDTO<ScreenDTO>
//        {
//            ImportedCount = deleted.Count,
//            Errors = errors,
//            Items = deleted
//        });
//    }

//    #endregion

//    #region 📥 Import / 📤 Export CSV

//    /// 📤 Export all screens to CSV
//    [HttpGet("export")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> ExportScreensAsCsv()
//    {
//        var query = _db.Screens.AsQueryable(); // 🧮 Base query

//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(s => s.CompanyId == _currentUser.CompanyId); // 🔐 Filter for Admins

//        var items = await query
//            .OrderBy(s => s.Name)                 // 🔤 Alphabetical order
//            .Select(ScreenMapper.ToScreenDto())   // 🧠 Project to DTO
//            .ToListAsync();                       // 🚀 Execute

//        using var writer = new StringWriter();                               // 📝 Write to memory
//        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture); // 🌍 Use invariant culture

//        csv.WriteRecords(items); // 🧾 Write all records

//        var bytes = Encoding.UTF8.GetBytes(writer.ToString()); // 🧱 Convert to bytes
//        return File(bytes, "text/csv", "screens_export.csv");  // 📤 Send file
//    }

//    /// 📥 Import screens from uploaded CSV file
//    [HttpPost("import")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenDTO>>> ImportScreens(IFormFile file)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("No company assigned to current user."); // ❌ Company check

//        if (file == null || file.Length == 0)
//            return BadRequest("No file uploaded."); // ❌ No file provided

//        var result = new BulkOperationResultDTO<ScreenDTO>(); // 📦 Result with errors + import count
//        using var reader = new StreamReader(file.OpenReadStream()); // 📂 Read stream
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture); // 🌍 Culture settings
//        csv.Context.RegisterClassMap<ScreenImportMap>(); // 🗺 Apply CSV mapping

//        var records = csv.GetRecords<ScreenImportDTO>().ToList(); // 📊 Read all rows

//        foreach (var (record, index) in records.Select((r, i) => (r, i)))
//        {
//            if (string.IsNullOrWhiteSpace(record.Name))
//            {
//                result.Errors.Add(new BulkOperationErrorDTO(index, "Screen name is required.", "Name", "REQUIRED"));
//                continue;
//            }

//            var entity = new Screen
//            {
//                Name = record.Name,
//                Identifier = record.Identifier,
//                Description = record.Description,
//                Type = record.Type,
//                ProjectId = record.ProjectId,
//                CompanyId = _currentUser.CompanyId.Value,
//                UserId = _currentUser.UserId,
//                CreatedAt = DateTime.UtcNow,
//                CreatedBy = _currentUser.UserId
//            };

//            _db.Screens.Add(entity);               // 💾 Add to context
//            await _db.SaveChangesAsync();          // 💾 Save entity
//            result.ImportedCount++;                // ➕ Count successful rows
//        }

//        result.TotalRows = records.Count;          // 🧮 Save total rows
//        return Ok(result);                         // ✅ Return import result
//    }

//    #endregion
//}
