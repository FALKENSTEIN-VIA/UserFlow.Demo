/// @file ScreenActionTypeController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-05
/// @brief API Controller for managing ScreenActionTypes with import/export and bulk operations
/// @details Handles CRUD operations, bulk processing, and data import/export functionality for ScreenActionType entities.
/// @endpoints
/// - GET /api/screen-action-types
/// - GET /api/screen-action-types/{id}
/// - POST /api/screen-action-types
/// - PUT /api/screen-action-types/{id}
/// - DELETE /api/screen-action-types/{id}
/// - POST /api/screen-action-types/bulk-create
/// - PUT /api/screen-action-types/bulk-update
/// - POST /api/screen-action-types/bulk-delete
/// - POST /api/screen-action-types/import
/// - GET /api/screen-action-types/export

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
[Route("api/screen-action-types")]
[Authorize(Roles = "Admin,GlobalAdmin")]
public class ScreenActionTypeController : ControllerBase
{
    #region 🔒 Private Fields

    private readonly AppDbContext _db; // 🗃️ Database context
    private readonly ICurrentUserService _currentUser; // 👤 User context
    private readonly ILogger<ScreenActionTypeController> _logger; // 📝 Logger

    #endregion

    #region 🔧 Constructor

    /// <summary>
    /// Initializes a new instance of the ScreenActionTypeController.
    /// </summary>
    public ScreenActionTypeController(AppDbContext db, ICurrentUserService currentUser, ILogger<ScreenActionTypeController> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    #endregion

    #region 📄 CRUD Operations

    /// <summary>
    /// 🔍 Retrieves all screen action types.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScreenActionTypeDTO>>> GetScreenActionTypes()
    {
        _logger.LogInformation("📄 Retrieved all screen action types by user {UserId}", _currentUser.UserId);

        var result = await _db.ScreenActionTypes
            .OrderBy(t => t.Name)
            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
            .ToListAsync();

        return Ok(result);
    }

    /// <summary>
    /// 🔍 Retrieves a single screen action type by ID.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ScreenActionTypeDTO>> GetScreenActionTypeById(long id)
    {
        _logger.LogInformation("🔎 Requested screen action type {Id} by user {UserId}", id, _currentUser.UserId);

        var result = await _db.ScreenActionTypes
            .Where(t => t.Id == id)
            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
            .FirstOrDefaultAsync();

        if (result == null)
        {
            _logger.LogWarning("❌ Screen action type {Id} not found", id);
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// ➕ Creates a new screen action type.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ScreenActionTypeDTO>> CreateScreenActionType(ScreenActionTypeCreateDTO dto)
    {
        _logger.LogInformation("➕ Creating screen action type '{Name}'", dto.Name);

        var entity = new ScreenActionType
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _db.ScreenActionTypes.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _db.ScreenActionTypes
            .Where(t => t.Id == entity.Id)
            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
            .FirstAsync();

        _logger.LogInformation("✅ Created screen action type '{Name}' (ID: {Id})", entity.Name, entity.Id);

        return CreatedAtAction(nameof(GetScreenActionTypeById), new { id = result.Id }, result);
    }

    /// <summary>
    /// ✏️ Updates an existing screen action type.
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateScreenActionType(long id, ScreenActionTypeUpdateDTO dto)
    {
        _logger.LogInformation("✏️ Updating screen action type {Id}", id);

        var entity = await _db.ScreenActionTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null)
        {
            _logger.LogWarning("❌ Screen action type {Id} not found for update", id);
            return NotFound();
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Updated screen action type {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// 🗑️ Soft deletes a screen action type.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteScreenActionType(long id)
    {
        _logger.LogInformation("🗑️ Deleting screen action type {Id}", id);

        var entity = await _db.ScreenActionTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null)
        {
            _logger.LogWarning("❌ Screen action type {Id} not found for deletion", id);
            return NotFound();
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Deleted screen action type {Id}", id);

        return NoContent();
    }

    #endregion

    #region 📦 Bulk Operations

    /// <summary>
    /// 🚀 Bulk creates multiple screen action types.
    /// </summary>
    [HttpPost("bulk-create")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionTypeDTO>>> BulkCreateTypes(List<ScreenActionTypeCreateDTO> dtos)
    {
        _logger.LogInformation("📦 Bulk creating {Count} screen action types", dtos.Count);

        var result = new BulkOperationResultDTO<ScreenActionTypeDTO>();

        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
                _logger.LogWarning("⚠️ Skipped record {Index}: Name is required", index);
                continue;
            }

            var entity = new ScreenActionType
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _db.ScreenActionTypes.Add(entity);
            await _db.SaveChangesAsync();

            var dtoResult = await _db.ScreenActionTypes
                .Where(t => t.Id == entity.Id)
                .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
                .FirstAsync();

            result.Items.Add(dtoResult);
            result.ImportedCount++;
        }

        _logger.LogInformation("✅ Bulk create completed: {Count} created", result.ImportedCount);

        return Ok(result);
    }

    #endregion

    #region 📥 Import / 📤 Export Operations

    /// <summary>
    /// 📥 Imports screen action types from CSV file.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionTypeDTO>>> ImportTypes(IFormFile file)
    {
        _logger.LogInformation("📥 Importing screen action types via CSV");

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("❌ No file uploaded for import.");
            return BadRequest("No file uploaded");
        }

        var result = new BulkOperationResultDTO<ScreenActionTypeDTO>();
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ScreenActionTypeImportMap>();

        var records = csv.GetRecords<ScreenActionTypeImportDTO>().ToList();

        foreach (var (record, index) in records.Select((r, i) => (r, i)))
        {
            if (string.IsNullOrWhiteSpace(record.Name))
            {
                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
                _logger.LogWarning("⚠️ Skipped row {Index}: Name missing", index);
                continue;
            }

            var entity = new ScreenActionType
            {
                Name = record.Name,
                Description = record.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _db.ScreenActionTypes.Add(entity);
            await _db.SaveChangesAsync();

            result.ImportedCount++;
        }

        _logger.LogInformation("✅ CSV import completed: {Count} records imported", result.ImportedCount);

        result.TotalRows = records.Count;
        return Ok(result);
    }

    /// <summary>
    /// 📤 Exports screen action types as CSV file.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportTypesAsCsv()
    {
        _logger.LogInformation("📤 Exporting screen action types to CSV");

        var items = await _db.ScreenActionTypes
            .OrderBy(t => t.Name)
            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
            .ToListAsync();

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(items);

        var bytes = Encoding.UTF8.GetBytes(writer.ToString());

        _logger.LogInformation("✅ Export completed: {Count} screen action types", items.Count);

        return File(bytes, "text/csv", "screenactiontypes_export.csv");
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

///// @file ScreenActionTypeController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-05
///// @brief API Controller for managing ScreenActionTypes with import/export and bulk operations
///// @details Handles CRUD operations, bulk processing, and data import/export functionality for ScreenActionType entities.
///// @endpoints
///// - GET /api/screen-action-types → Get all screen action types
///// - GET /api/screen-action-types/{id} → Get single type by ID
///// - POST /api/screen-action-types → Create new type
///// - PUT /api/screen-action-types/{id} → Update existing type
///// - DELETE /api/screen-action-types/{id} → Soft delete type
///// - POST /api/screen-action-types/bulk-create → Bulk create types
///// - PUT /api/screen-action-types/bulk-update → Bulk update types
///// - POST /api/screen-action-types/bulk-delete → Bulk delete types
///// - POST /api/screen-action-types/import → Import types from CSV
///// - GET /api/screen-action-types/export → Export types as CSV

//[ApiController]
//[Route("api/screen-action-types")]
//[Authorize(Roles = "Admin,GlobalAdmin")]
//public class ScreenActionTypeController : ControllerBase
//{
//    #region 🔒 Private Fields
//    private readonly AppDbContext _db;
//    private readonly ICurrentUserService _currentUser;
//    #endregion

//    #region 🔧 Constructor
//    /// <summary>
//    /// Initializes a new instance of the ScreenActionTypeController
//    /// </summary>
//    public ScreenActionTypeController(AppDbContext db, ICurrentUserService currentUser)
//    {
//        _db = db;
//        _currentUser = currentUser;
//    }
//    #endregion

//    #region 📄 CRUD Operations
//    /// <summary>
//    /// 🔍 Retrieves all screen action types
//    /// </summary>
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<ScreenActionTypeDTO>>> GetScreenActionTypes()
//    {
//        var result = await _db.ScreenActionTypes
//            .OrderBy(t => t.Name)
//            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
//            .ToListAsync();

//        return Ok(result);
//    }

//    /// <summary>
//    /// 🔍 Retrieves single screen action type by ID
//    /// </summary>
//    [HttpGet("{id:long}")]
//    public async Task<ActionResult<ScreenActionTypeDTO>> GetScreenActionTypeById(long id)
//    {
//        var result = await _db.ScreenActionTypes
//            .Where(t => t.Id == id)
//            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
//            .FirstOrDefaultAsync();

//        return result == null ? NotFound() : Ok(result);
//    }

//    /// <summary>
//    /// 🚀 Creates new screen action type (Admin restricted)
//    /// </summary>
//    [HttpPost]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<ScreenActionTypeDTO>> CreateScreenActionType(ScreenActionTypeCreateDTO dto)
//    {
//        var entity = new ScreenActionType
//        {
//            Name = dto.Name,
//            Description = dto.Description,
//            CreatedAt = DateTime.UtcNow,
//            CreatedBy = _currentUser.UserId
//        };

//        _db.ScreenActionTypes.Add(entity);
//        await _db.SaveChangesAsync();

//        /// 🔄 Return created entity as DTO
//        var result = await _db.ScreenActionTypes
//            .Where(t => t.Id == entity.Id)
//            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
//            .FirstAsync();

//        return CreatedAtAction(nameof(GetScreenActionTypeById), new { id = result.Id }, result);
//    }

//    /// <summary>
//    /// 🔄 Updates existing screen action type (Admin restricted)
//    /// </summary>
//    [HttpPut("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> UpdateScreenActionType(long id, ScreenActionTypeUpdateDTO dto)
//    {
//        var entity = await _db.ScreenActionTypes.FirstOrDefaultAsync(t => t.Id == id);
//        if (entity == null) return NotFound();

//        /// 📦 Update entity fields
//        entity.Name = dto.Name;
//        entity.Description = dto.Description;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();
//        return NoContent();
//    }

//    /// <summary>
//    /// 🗑️ Soft deletes screen action type (Admin restricted)
//    /// </summary>
//    [HttpDelete("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> DeleteScreenActionType(long id)
//    {
//        var entity = await _db.ScreenActionTypes.FirstOrDefaultAsync(t => t.Id == id);
//        if (entity == null) return NotFound();

//        /// ⚠️ Mark as deleted
//        entity.IsDeleted = true;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();
//        return NoContent();
//    }
//    #endregion

//    #region 📦 Bulk Operations
//    /// <summary>
//    /// 🚀 Bulk creates multiple screen action types (Admin restricted)
//    /// </summary>
//    [HttpPost("bulk-create")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionTypeDTO>>> BulkCreateTypes(List<ScreenActionTypeCreateDTO> dtos)
//    {
//        var result = new BulkOperationResultDTO<ScreenActionTypeDTO>();
//        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
//        {
//            /// ⚠️ Validate required fields
//            if (string.IsNullOrWhiteSpace(dto.Name))
//            {
//                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
//                continue;
//            }

//            var entity = new ScreenActionType
//            {
//                Name = dto.Name,
//                Description = dto.Description,
//                CreatedAt = DateTime.UtcNow,
//                CreatedBy = _currentUser.UserId
//            };

//            _db.ScreenActionTypes.Add(entity);
//            await _db.SaveChangesAsync();

//            var dtoResult = await _db.ScreenActionTypes
//                .Where(t => t.Id == entity.Id)
//                .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
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
//    /// 📥 Imports screen action types from CSV file (Admin restricted)
//    /// </summary>
//    [HttpPost("import")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<BulkOperationResultDTO<ScreenActionTypeDTO>>> ImportTypes(IFormFile file)
//    {
//        if (file == null || file.Length == 0)
//            return BadRequest("❌ No file uploaded");

//        var result = new BulkOperationResultDTO<ScreenActionTypeDTO>();
//        using var reader = new StreamReader(file.OpenReadStream());
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
//        csv.Context.RegisterClassMap<ScreenActionTypeImportMap>();

//        var records = csv.GetRecords<ScreenActionTypeImportDTO>().ToList();
//        foreach (var (record, index) in records.Select((r, i) => (r, i)))
//        {
//            /// ⚠️ Validate required fields
//            if (string.IsNullOrWhiteSpace(record.Name))
//            {
//                result.Errors.Add(new(index, "Name is required", "Name", "REQUIRED"));
//                continue;
//            }

//            var entity = new ScreenActionType
//            {
//                Name = record.Name,
//                Description = record.Description,
//                CreatedAt = DateTime.UtcNow,
//                CreatedBy = _currentUser.UserId
//            };

//            _db.ScreenActionTypes.Add(entity);
//            await _db.SaveChangesAsync();
//            result.ImportedCount++;
//        }

//        return Ok(result);
//    }

//    /// <summary>
//    /// 📤 Exports screen action types as CSV file (Admin restricted)
//    /// </summary>
//    [HttpGet("export")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> ExportTypesAsCsv()
//    {
//        var items = await _db.ScreenActionTypes
//            .OrderBy(t => t.Name)
//            .Select(ScreenActionTypeMapper.ToScreenActionTypeDto())
//            .ToListAsync();

//        using var writer = new StringWriter();
//        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
//        csv.WriteRecords(items);

//        var bytes = Encoding.UTF8.GetBytes(writer.ToString());
//        return File(bytes, "text/csv", "screenactiontypes_export.csv");
//    }
//    #endregion
//}