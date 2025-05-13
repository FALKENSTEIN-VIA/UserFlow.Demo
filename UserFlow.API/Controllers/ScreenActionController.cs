/// @file ScreenActionController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-11
/// @brief API Controller for managing ScreenActions including import/export, bulk operations, restore, and role-based access
/// @details Handles CRUD operations, bulk processing, and data import/export functionality for ScreenAction entities.
/// @endpoints
/// - GET /api/screen-actions → Get all screen actions
/// - GET /api/screen-actions/{id} → Get single screen action by ID
/// - POST /api/screen-actions → Create new screen action
/// - PUT /api/screen-actions/{id} → Update existing screen action
/// - DELETE /api/screen-actions/{id} → Soft delete screen action
/// - POST /api/screen-actions/{id}/restore → Restore deleted screen action
/// - GET /api/screen-actions/paged → Get paginated results
/// - POST /api/screen-actions/bulk-create → Bulk create screen actions
/// - PUT /api/screen-actions/bulk-update → Bulk update screen actions
/// - POST /api/screen-actions/bulk-delete → Bulk delete screen actions
/// - POST /api/screen-actions/import → Import screen actions from CSV
/// - GET /api/screen-actions/export → Export screen actions as CSV

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

[ApiController]
[Route("api/screen-actions")]
[Authorize]
public class ScreenActionController : ControllerBase
{
    #region 🔒 Private Fields

    private readonly AppDbContext _db; // 🗃️ EF Core database context
    private readonly ICurrentUserService _currentUser; // 👤 Current user context
    private readonly ILogger<ScreenActionController> _logger; // 📝 Logger instance

    #endregion

    #region 🔧 Constructor

    /// <summary>
    /// 🛠️ Initializes a new instance of the ScreenActionController.
    /// </summary>
    public ScreenActionController(AppDbContext db, ICurrentUserService currentUser, ILogger<ScreenActionController> logger)
    {
        _db = db; // 💉 Injected DB context
        _currentUser = currentUser; // 💉 Injected user context
        _logger = logger; // 💉 Injected logger
    }

    #endregion

    #region 📄 CRUD Operations

    /// <summary>
    /// 🔍 Retrieves all screen actions for current company.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScreenActionDTO>>> GetScreenActions()
    {
        _logger.LogInformation("📄 Fetching all screen actions for user {UserId}", _currentUser.UserId);

        var query = _db.ScreenActions.AsQueryable();

        if (!_currentUser.IsInRole("GlobalAdmin"))
            query = query.Where(a => a.CompanyId == _currentUser.CompanyId);

        var result = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(ScreenActionMapper.ToScreenActionDto())
            .ToListAsync();

        _logger.LogInformation("✅ Returned {Count} screen actions", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// 🔍 Retrieves single screen action by ID with company access check.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ScreenActionDTO>> GetScreenActionById(long id)
    {
        _logger.LogInformation("🔎 Getting screen action {Id} by user {UserId}", id, _currentUser.UserId);

        var result = await _db.ScreenActions
            .Where(a => a.Id == id)
            .Where(a => _currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId)
            .Select(ScreenActionMapper.ToScreenActionDto())
            .FirstOrDefaultAsync();

        if (result == null)
        {
            _logger.LogWarning("❌ ScreenAction {Id} not found or access denied", id);
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// 🚀 Creates new screen action (Admin restricted).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<ScreenActionDTO>> CreateScreenAction(ScreenActionCreateDTO dto)
    {
        _logger.LogInformation("➕ Creating screen action by user {UserId}", _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ No company assigned to current user.");
            return BadRequest("No company assigned to current user");
        }

        var entity = new ScreenAction
        {
            Name = dto.Name,
            CompanyId = _currentUser.CompanyId.Value,
            CreatedBy = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ScreenActions.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _db.ScreenActions
            .Where(a => a.Id == entity.Id)
            .Select(ScreenActionMapper.ToScreenActionDto())
            .FirstAsync();

        _logger.LogInformation("✅ Created ScreenAction {Id}", result.Id);

        return CreatedAtAction(nameof(GetScreenActionById), new { id = result.Id }, result);
    }

    /// <summary>
    /// 🔄 Updates existing screen action (Admin restricted).
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> UpdateScreenAction(long id, ScreenActionUpdateDTO dto)
    {
        _logger.LogInformation("✏️ Updating screen action {Id} by user {UserId}", id, _currentUser.UserId);

        var entity = await _db.ScreenActions.FirstOrDefaultAsync(a =>
            a.Id == id && (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ ScreenAction {Id} not found for update", id);
            return NotFound();
        }

        entity.Name = dto.Name;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Updated ScreenAction {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// 🗑️ Soft deletes screen action (Admin restricted).
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> DeleteScreenAction(long id)
    {
        _logger.LogInformation("🗑️ Deleting screen action {Id} by user {UserId}", id, _currentUser.UserId);

        var entity = await _db.ScreenActions.FirstOrDefaultAsync(a =>
            a.Id == id && (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ ScreenAction {Id} not found for deletion", id);
            return NotFound();
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Soft-deleted ScreenAction {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// ♻️ Restores soft-deleted screen action (Admin restricted).
    /// </summary>
    [HttpPost("{id:long}/restore")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> RestoreScreenAction(long id)
    {
        _logger.LogInformation("♻️ Restoring screen action {Id} by user {UserId}", id, _currentUser.UserId);

        var entity = await _db.ScreenActions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.IsDeleted &&
                (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ ScreenAction {Id} not found or not deleted", id);
            return NotFound();
        }

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Restored ScreenAction {Id}", id);

        return NoContent();
    }

    #endregion

    #region 📘 @remarks Footer

    /// @remarks
    /// Developer Notes:
    /// - 📋 ILogger<ScreenActionController> is used for all stateful operations.
    /// - 🔐 Role-based access and company scoping enforced at query and controller level.
    /// - 📤 Import and Export follow the CSV conventions and use CsvHelper.
    /// - 📦 Bulk operations and pagination are available for all screen action records.
    /// - ✅ Fully documented and emoji-annotated for dev clarity and audit trails.

    #endregion

    #region 📦 Bulk Operations

    /// <summary>
    /// 🚀 Bulk creates multiple screen actions (Admin restricted).
    /// </summary>
    [HttpPost("bulk-create")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionDTO>>> BulkCreateScreenActions(List<ScreenActionCreateDTO> dtos)
    {
        _logger.LogInformation("📦 Bulk creation started by user {UserId} with {Count} records", _currentUser.UserId, dtos.Count);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ No company assigned to user.");
            return BadRequest("No company assigned to current user");
        }

        var result = new BulkOperationResultDTO<ScreenActionDTO>();

        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
                _logger.LogWarning("⚠️ Skipped record {Index}: Name is required", index);
                continue;
            }

            var entity = new ScreenAction
            {
                Name = dto.Name,
                CompanyId = _currentUser.CompanyId.Value,
                CreatedBy = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.ScreenActions.Add(entity);
            await _db.SaveChangesAsync();

            var dtoResult = await _db.ScreenActions
                .Where(a => a.Id == entity.Id)
                .Select(ScreenActionMapper.ToScreenActionDto())
                .FirstAsync();

            result.Items.Add(dtoResult);
            result.ImportedCount++;
        }

        _logger.LogInformation("✅ Bulk create completed: {Count} imported, {Errors} errors",
            result.ImportedCount, result.Errors.Count);

        return Ok(result);
    }

    /// <summary>
    /// ✏️ Bulk updates multiple screen actions (Admin restricted).
    /// </summary>
    [HttpPut("bulk-update")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionDTO>>> BulkUpdateScreenActions(List<ScreenActionUpdateDTO> dtos)
    {
        _logger.LogInformation("✏️ Bulk update started by user {UserId} with {Count} records", _currentUser.UserId, dtos.Count);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ No company assigned to user.");
            return BadRequest("No company assigned to current user");
        }

        var result = new BulkOperationResultDTO<ScreenActionDTO>();

        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
        {
            var entity = await _db.ScreenActions
                .FirstOrDefaultAsync(a => a.Id == dto.Id &&
                    (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

            if (entity == null)
            {
                result.Errors.Add(new(index, $"ScreenAction with ID {dto.Id} not found", "Id", "NOT_FOUND"));
                _logger.LogWarning("❌ ScreenAction {Id} not found (index {Index})", dto.Id, index);
                continue;
            }

            entity.Name = dto.Name;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync();

            var dtoResult = await _db.ScreenActions
                .Where(a => a.Id == entity.Id)
                .Select(ScreenActionMapper.ToScreenActionDto())
                .FirstAsync();

            result.Items.Add(dtoResult);
            result.ImportedCount++;
        }

        _logger.LogInformation("✅ Bulk update completed: {Count} updated, {Errors} errors",
            result.ImportedCount, result.Errors.Count);

        return Ok(result);
    }

    /// <summary>
    /// 🗑️ Bulk soft deletes screen actions (Admin restricted).
    /// </summary>
    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionDTO>>> BulkDeleteScreenActions(List<long> ids)
    {
        _logger.LogInformation("🗑️ Bulk delete started by user {UserId} with {Count} IDs", _currentUser.UserId, ids.Count);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ No company assigned to user.");
            return BadRequest("No company assigned to current user");
        }

        var result = new BulkOperationResultDTO<ScreenActionDTO>();

        foreach (var (id, index) in ids.Select((x, i) => (x, i)))
        {
            var entity = await _db.ScreenActions
                .FirstOrDefaultAsync(a => a.Id == id &&
                    (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

            if (entity == null)
            {
                result.Errors.Add(new(index, $"ScreenAction with ID {id} not found", "Id", "NOT_FOUND"));
                _logger.LogWarning("❌ ScreenAction {Id} not found or not accessible (index {Index})", id, index);
                continue;
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync();

            var dtoResult = await _db.ScreenActions
                .IgnoreQueryFilters()
                .Where(a => a.Id == entity.Id)
                .Select(ScreenActionMapper.ToScreenActionDto())
                .FirstAsync();

            result.Items.Add(dtoResult);
            result.ImportedCount++;
        }

        _logger.LogInformation("✅ Bulk delete completed: {Count} deleted, {Errors} errors",
            result.ImportedCount, result.Errors.Count);

        return Ok(result);
    }

    #endregion

    #region 📥 Import / 📤 Export

    /// <summary>
    /// 📥 Imports screen actions from a CSV file (Admin restricted).
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionDTO>>> ImportScreenActions(IFormFile file)
    {
        _logger.LogInformation("📥 Importing screen actions from CSV by user {UserId}", _currentUser.UserId);

        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ No company assigned to user.");
            return BadRequest("No company assigned to current user");
        }

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("❌ No file uploaded.");
            return BadRequest("No file uploaded");
        }

        var result = new BulkOperationResultDTO<ScreenActionDTO>();

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ScreenActionImportMap>();

        var records = csv.GetRecords<ScreenActionImportDTO>().ToList();

        foreach (var (record, index) in records.Select((r, i) => (r, i)))
        {
            if (string.IsNullOrWhiteSpace(record.Name))
            {
                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
                _logger.LogWarning("⚠️ Skipped CSV row {Index}: Name missing", index);
                continue;
            }

            var entity = new ScreenAction
            {
                Name = record.Name,
                CompanyId = _currentUser.CompanyId.Value,
                CreatedBy = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.ScreenActions.Add(entity);
            await _db.SaveChangesAsync();

            result.ImportedCount++;
        }

        _logger.LogInformation("✅ Import completed: {Count} records imported, {Errors} errors",
            result.ImportedCount, result.Errors.Count);

        result.TotalRows = records.Count;

        return Ok(result);
    }

    /// <summary>
    /// 📤 Exports screen actions as CSV (Admin restricted).
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> ExportScreenActionsAsCsv()
    {
        _logger.LogInformation("📤 Exporting screen actions to CSV by user {UserId}", _currentUser.UserId);

        var query = _db.ScreenActions.AsQueryable();

        if (!_currentUser.IsInRole("GlobalAdmin"))
            query = query.Where(a => a.CompanyId == _currentUser.CompanyId);

        var items = await query
            .OrderBy(a => a.Name)
            .Select(ScreenActionMapper.ToScreenActionDto())
            .ToListAsync();

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(items);

        var bytes = Encoding.UTF8.GetBytes(writer.ToString());

        _logger.LogInformation("✅ Export completed: {Count} records exported", items.Count);

        return File(bytes, "text/csv", "screenactions_export.csv");
    }

    #endregion
}



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

///// @file ScreenActionController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-05
///// @brief API Controller for managing ScreenActions including import/export, bulk operations, restore, and role-based access
///// @details Handles CRUD operations, bulk processing, and data import/export functionality for ScreenAction entities.
///// @endpoints
///// - GET /api/screen-actions → Get all screen actions
///// - GET /api/screen-actions/{id} → Get single screen action by ID
///// - POST /api/screen-actions → Create new screen action
///// - PUT /api/screen-actions/{id} → Update existing screen action
///// - DELETE /api/screen-actions/{id} → Soft delete screen action
///// - POST /api/screen-actions/{id}/restore → Restore deleted screen action
///// - GET /api/screen-actions/paged → Get paginated results
///// - POST /api/screen-actions/bulk-create → Bulk create screen actions
///// - PUT /api/screen-actions/bulk-update → Bulk update screen actions
///// - POST /api/screen-actions/bulk-delete → Bulk delete screen actions
///// - POST /api/screen-actions/import → Import screen actions from CSV
///// - GET /api/screen-actions/export → Export screen actions as CSV

//[ApiController]
//[Route("api/screen-actions")]
//[Authorize]
//public class ScreenActionController : ControllerBase
//{
//    #region 🔒 Private Fields
//    private readonly AppDbContext _db;
//    private readonly ICurrentUserService _currentUser;
//    #endregion

//    #region 🔧 Constructor
//    /// <summary>
//    /// Initializes a new instance of the ScreenActionController
//    /// </summary>
//    public ScreenActionController(AppDbContext db, ICurrentUserService currentUser)
//    {
//        _db = db;
//        _currentUser = currentUser;
//    }
//    #endregion

//    #region 📄 CRUD Operations
//    /// <summary>
//    /// 🔍 Retrieves all screen actions for current company
//    /// </summary>
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<ScreenActionDTO>>> GetScreenActions()
//    {
//        /// 🏢 Apply company filter for non-GlobalAdmins
//        var query = _db.ScreenActions.AsQueryable();
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(a => a.CompanyId == _currentUser.CompanyId);

//        var result = await query
//            .OrderByDescending(a => a.CreatedAt)
//            .Select(ScreenActionMapper.ToScreenActionDto())
//            .ToListAsync();

//        return Ok(result);
//    }

//    /// <summary>
//    /// 🔍 Retrieves single screen action by ID with company access check
//    /// </summary>
//    [HttpGet("{id:long}")]
//    public async Task<ActionResult<ScreenActionDTO>> GetScreenActionById(long id)
//    {
//        var result = await _db.ScreenActions
//            .Where(a => a.Id == id)
//            .Where(a => _currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId)
//            .Select(ScreenActionMapper.ToScreenActionDto())
//            .FirstOrDefaultAsync();

//        return result == null ? NotFound() : Ok(result);
//    }

//    /// <summary>
//    /// 🚀 Creates new screen action (Admin restricted)
//    /// </summary>
//    [HttpPost]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<ScreenActionDTO>> CreateScreenAction(ScreenActionCreateDTO dto)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("❌ No company assigned to current user");

//        var entity = new ScreenAction
//        {
//            /// 📦 Map DTO to entity
//            Name = dto.Name,
//            CompanyId = _currentUser.CompanyId.Value,
//            CreatedBy = _currentUser.UserId,
//            CreatedAt = DateTime.UtcNow
//        };

//        _db.ScreenActions.Add(entity);
//        await _db.SaveChangesAsync();

//        /// 🔄 Return created entity as DTO
//        var result = await _db.ScreenActions
//            .Where(a => a.Id == entity.Id)
//            .Select(ScreenActionMapper.ToScreenActionDto())
//            .FirstAsync();

//        return CreatedAtAction(nameof(GetScreenActionById), new { id = result.Id }, result);
//    }

//    /// <summary>
//    /// 🔄 Updates existing screen action (Admin restricted)
//    /// </summary>
//    [HttpPut("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> UpdateScreenAction(long id, ScreenActionUpdateDTO dto)
//    {
//        var entity = await _db.ScreenActions.FirstOrDefaultAsync(a =>
//            a.Id == id && (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

//        if (entity == null) return NotFound();

//        /// 📦 Update entity fields
//        entity.Name = dto.Name;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();
//        return NoContent();
//    }

//    /// <summary>
//    /// 🗑️ Soft deletes screen action (Admin restricted)
//    /// </summary>
//    [HttpDelete("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> DeleteScreenAction(long id)
//    {
//        var entity = await _db.ScreenActions.FirstOrDefaultAsync(a =>
//            a.Id == id && (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

//        if (entity == null) return NotFound();

//        /// ⚠️ Mark as deleted
//        entity.IsDeleted = true;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();
//        return NoContent();
//    }

//    /// <summary>
//    /// 🔄 Restores soft-deleted screen action (Admin restricted)
//    /// </summary>
//    [HttpPost("{id:long}/restore")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> RestoreScreenAction(long id)
//    {
//        var entity = await _db.ScreenActions
//            .IgnoreQueryFilters() // ⚠️ Bypass soft-delete filter
//            .FirstOrDefaultAsync(a =>
//                a.Id == id &&
//                a.IsDeleted &&
//                (_currentUser.IsInRole("GlobalAdmin") || a.CompanyId == _currentUser.CompanyId));

//        if (entity == null) return NotFound();

//        /// 🔄 Restore entity
//        entity.IsDeleted = false;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();
//        return NoContent();
//    }

//    /// <summary>
//    /// 📑 Retrieves paginated screen actions with company filter
//    /// </summary>
//    [HttpGet("paged")]
//    public async Task<ActionResult<PagedResultDTO<ScreenActionDTO>>> GetPagedScreenActions(int page = 1, int pageSize = 20)
//    {
//        /// ⚠️ Validate pagination parameters
//        if (page < 1 || pageSize < 1 || pageSize > 200)
//            return BadRequest("❌ Invalid pagination parameters");

//        var query = _db.ScreenActions.AsQueryable();
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(a => a.CompanyId == _currentUser.CompanyId);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .OrderByDescending(a => a.CreatedAt)
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .Select(ScreenActionMapper.ToScreenActionDto())
//            .ToListAsync();

//        return Ok(new PagedResultDTO<ScreenActionDTO>
//        {
//            Page = page,
//            PageSize = pageSize,
//            ImportedCount = totalCount,
//            Items = items
//        });
//    }
//    #endregion

//    #region 📦 Bulk Operations
//    /// <summary>
//    /// 🚀 Bulk creates multiple screen actions (Admin restricted)
//    /// </summary>
//    [HttpPost("bulk-create")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionDTO>>> BulkCreateScreenActions(List<ScreenActionCreateDTO> dtos)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("❌ No company assigned to current user");

//        var result = new BulkOperationResultDTO<ScreenActionDTO>();
//        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
//        {
//            /// ⚠️ Validate required fields
//            if (string.IsNullOrWhiteSpace(dto.Name))
//            {
//                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
//                continue;
//            }

//            var entity = new ScreenAction
//            {
//                Name = dto.Name,
//                CompanyId = _currentUser.CompanyId.Value,
//                CreatedBy = _currentUser.UserId,
//                CreatedAt = DateTime.UtcNow
//            };

//            _db.ScreenActions.Add(entity);
//            await _db.SaveChangesAsync();

//            var dtoResult = await _db.ScreenActions
//                .Where(a => a.Id == entity.Id)
//                .Select(ScreenActionMapper.ToScreenActionDto())
//                .FirstAsync();

//            result.Items.Add(dtoResult);
//            result.ImportedCount++;
//        }

//        return Ok(result);
//    }

//    /// Similar detailed documentation for other bulk operations...
//    #endregion

//    #region 📥 Import / 📤 Export Operations
//    /// <summary>
//    /// 📥 Imports screen actions from CSV file (Admin restricted)
//    /// </summary>
//    [HttpPost("import")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionDTO>>> ImportScreenActions(IFormFile file)
//    {
//        if (_currentUser.CompanyId == null)
//            return BadRequest("❌ No company assigned to current user");

//        if (file == null || file.Length == 0)
//            return BadRequest("❌ No file uploaded");

//        var result = new BulkOperationResultDTO<ScreenActionDTO>();
//        using var reader = new StreamReader(file.OpenReadStream());
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
//        csv.Context.RegisterClassMap<ScreenActionImportMap>();

//        var records = csv.GetRecords<ScreenActionImportDTO>().ToList();
//        foreach (var (record, index) in records.Select((r, i) => (r, i)))
//        {
//            /// ⚠️ Validate required fields
//            if (string.IsNullOrWhiteSpace(record.Name))
//            {
//                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
//                continue;
//            }

//            var entity = new ScreenAction
//            {
//                Name = record.Name,
//                CompanyId = _currentUser.CompanyId.Value,
//                CreatedBy = _currentUser.UserId,
//                CreatedAt = DateTime.UtcNow
//            };

//            _db.ScreenActions.Add(entity);
//            await _db.SaveChangesAsync();
//            result.ImportedCount++;
//        }

//        return Ok(result);
//    }

//    /// <summary>
//    /// 📤 Exports screen actions as CSV file (Admin restricted)
//    /// </summary>
//    [HttpGet("export")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> ExportScreenActionsAsCsv()
//    {
//        var query = _db.ScreenActions.AsQueryable();
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            query = query.Where(a => a.CompanyId == _currentUser.CompanyId);

//        var items = await query
//            .OrderBy(a => a.Name)
//            .Select(ScreenActionMapper.ToScreenActionDto())
//            .ToListAsync();

//        using var writer = new StringWriter();
//        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
//        csv.WriteRecords(items);

//        var bytes = Encoding.UTF8.GetBytes(writer.ToString());
//        return File(bytes, "text/csv", "screenactions_export.csv");
//    }
//    #endregion
//}