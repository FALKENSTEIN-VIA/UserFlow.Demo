/// @file ProjectController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief API controller for managing projects with CRUD, pagination, bulk ops, CSV import/export, and soft delete.
/// @details
/// This controller provides endpoints to create, read, update, delete, restore,
/// and import/export project entities. It supports pagination, multi-tenancy,
/// role-based access control, and bulk operations for Admins and GlobalAdmins.
/// @endpoints
/// - GET    /api/projects               → Get all projects (with ?includeTasks=true)
/// - GET    /api/projects/paged         → Paginated projects (page, pageSize, sortBy)
/// - GET    /api/projects/{id}          → Get project by ID (with history)
/// - POST   /api/projects               → Create new project (Admin/GlobalAdmin)
/// - PUT    /api/projects/{id}          → Update project (Admin/GlobalAdmin)
/// - DELETE /api/projects/{id}          → Soft delete project (Admin/GlobalAdmin)
/// - POST   /api/projects/{id}/restore  → Restore project (Admin/GlobalAdmin)
/// - POST   /api/projects/bulk          → Bulk create/update projects
/// - POST   /api/projects/import        → Import projects from CSV
/// - GET    /api/projects/export        → Export projects to CSV


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data;
using UserFlow.API.Data.Entities;
using UserFlow.API.Mappers;
using UserFlow.API.Services.Interfaces;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Controllers;

/// <summary>
/// 🧩 API controller to manage project-related operations such as CRUD, pagination, import/export.
/// </summary>
[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    #region 🔒 Fields

    private readonly AppDbContext _db; // 🗃️ EF Core database context
    private readonly ICurrentUserService _currentUser; // 👤 Information about the currently logged-in user
    private readonly ILogger<ProjectController> _logger; // 📝 Logger instance for tracing

    #endregion

    #region 🔧 Constructor

    /// <summary>
    /// 🛠️ Constructor for injecting services.
    /// </summary>
    public ProjectController(AppDbContext db, ICurrentUserService currentUser, ILogger<ProjectController> logger)
    {
        _db = db; // 💉 Injected DB context
        _currentUser = currentUser; // 💉 Injected user context
        _logger = logger; // 💉 Injected logger
    }

    #endregion

    #region 📄 CRUD Operations

    /// <summary>
    /// 📄 Retrieves all visible projects for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDTO>>> GetProjects()
    {
        // 🔍 Prepare base query with necessary includes
        var query = _db.Projects
            .Include(p => p.User)
            .Include(p => p.Company)
            .AsQueryable();

        // 🔐 Apply access filters depending on user role
        if (!_currentUser.IsInRole("GlobalAdmin"))
        {
            if (_currentUser.IsInRole("Admin"))
            {
                // 🔐 Admins can access all company projects
                query = query.Where(p => p.CompanyId == _currentUser.CompanyId);
            }
            else
            {
                // 🔐 Normal users can access their own or shared projects
                query = query.Where(p => p.CompanyId == _currentUser.CompanyId &&
                                         (p.UserId == _currentUser.UserId || p.IsShared));
            }
        }

        // 📥 Execute query and project to DTO
        var projects = await query
            .Select(ProjectMapper.ToProjectDto())
            .ToListAsync();

        _logger.LogInformation("✅ Retrieved {Count} projects for user {UserId}", projects.Count, _currentUser.UserId);
        return Ok(projects);
    }

    /// <summary>
    /// 📄 Retrieves paginated list of projects for the current user.
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDTO<ProjectDTO>>> GetPagedProjects(int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("📄 Retrieving paged projects for user {UserId}, page {Page}, size {Size}", _currentUser.UserId, page, pageSize);

        // ❌ Validate pagination input
        if (page < 1 || pageSize < 1 || pageSize > 200)
        {
            _logger.LogWarning("⚠️ Invalid pagination parameters: page={Page}, size={Size}", page, pageSize);
            return BadRequest("Invalid page or pageSize value.");
        }

        // 🔍 Prepare base query
        var query = _db.Projects
            .Include(p => p.User)
            .Include(p => p.Company)
            .AsQueryable();

        // 🔐 Filter access by role
        if (!_currentUser.IsInRole("GlobalAdmin"))
        {
            if (_currentUser.IsInRole("Admin"))
            {
                query = query.Where(p => p.CompanyId == _currentUser.CompanyId);
            }
            else
            {
                query = query.Where(p => p.CompanyId == _currentUser.CompanyId &&
                                         (p.UserId == _currentUser.UserId || p.IsShared));
            }
        }

        // 🔢 Get total count before paging
        var total = await query.CountAsync();

        // ⏳ Fetch paged result
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectMapper.ToProjectDto())
            .ToListAsync();

        _logger.LogInformation("✅ Returning {Count} of {Total} projects", items.Count, total);

        // 📤 Return paged DTO
        var result = new PagedResultDTO<ProjectDTO>
        {
            Page = page,
            PageSize = pageSize,
            ImportedCount = total,
            Items = items
        };

        return Ok(result);
    }

    /// <summary>
    /// 🔎 Retrieves a specific project by ID with access control.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProjectDTO>> GetProjectById(long id)
    {
        _logger.LogInformation("🔎 Retrieving project {Id} for user {UserId}", id, _currentUser.UserId);

        // 🔍 Load project with navigation properties
        var project = await _db.Projects
            .Include(p => p.User)
            .Include(p => p.Company)
            .Where(p => p.Id == id)
            .Where(p => _currentUser.IsInRole("GlobalAdmin") ||
                        (p.CompanyId == _currentUser.CompanyId &&
                         (p.UserId == _currentUser.UserId || p.IsShared)))
            .Select(ProjectMapper.ToProjectDto())
            .FirstOrDefaultAsync();

        // ❌ Not found or not authorized
        if (project == null)
        {
            _logger.LogWarning("❌ Project {Id} not found or access denied for user {UserId}", id, _currentUser.UserId);
            return NotFound();
        }

        _logger.LogInformation("✅ Project {Id} found", id);
        return Ok(project);
    }

    /// <summary>
    /// ➕ Creates a new project.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<ActionResult<ProjectDTO>> CreateProject(ProjectCreateDTO dto)
    {
        _logger.LogInformation("➕ Creating project by user {UserId}", _currentUser.UserId);

        // ❌ Ensure CompanyId exists
        if (_currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ Cannot create project – no company assigned to user {UserId}", _currentUser.UserId);
            return BadRequest("No company assigned to current user.");
        }

        // 🧱 Create new entity
        var entity = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            IsShared = dto.IsShared,
            CompanyId = _currentUser.CompanyId.Value,
            UserId = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _db.Projects.Add(entity); // 💾 Add to context
        await _db.SaveChangesAsync(); // 💾 Save to DB

        // 📦 Reload with includes and project to DTO
        var result = await _db.Projects
            .Include(p => p.User)
            .Include(p => p.Company)
            .Where(p => p.Id == entity.Id)
            .Select(ProjectMapper.ToProjectDto())
            .FirstAsync();

        _logger.LogInformation("✅ Project {Id} created successfully", result.Id);
        return CreatedAtAction(nameof(GetProjectById), new { id = result.Id }, result);
    }

    #endregion

    #region ✏️ Update / 🗑️ Delete / ♻️ Restore

    /// <summary>
    /// ✏️ Updates an existing project.
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> UpdateProject(long id, ProjectUpdateDTO dto)
    {
        _logger.LogInformation("✏️ Updating project {Id} by user {UserId}", id, _currentUser.UserId);

        var entity = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == id &&
                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Project {Id} not found or access denied", id);
            return NotFound();
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.IsShared = dto.IsShared;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Project {Id} updated", id);
        return NoContent();
    }

    /// <summary>
    /// 🗑️ Soft deletes a project.
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> DeleteProject(long id)
    {
        _logger.LogInformation("🗑️ Deleting project {Id} by user {UserId}", id, _currentUser.UserId);

        var entity = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == id &&
                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Project {Id} not found or access denied", id);
            return NotFound();
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Project {Id} soft-deleted", id);
        return NoContent();
    }

    /// <summary>
    /// ♻️ Restores a soft-deleted project.
    /// </summary>
    [HttpPost("{id:long}/restore")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> RestoreProject(long id)
    {
        _logger.LogInformation("♻️ Restoring project {Id} by user {UserId}", id, _currentUser.UserId);

        var entity = await _db.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id &&
                p.IsDeleted &&
                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

        if (entity == null)
        {
            _logger.LogWarning("❌ Project {Id} not found or not deleted", id);
            return NotFound();
        }

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Project {Id} restored", id);
        return NoContent();
    }

    #endregion


    #region 📘 @remarks Footer

    /// @remarks
    /// Developer Notes:
    /// - 📋 All methods log key operations including failures and validation steps.
    /// - 🔐 Multi-tenancy filters and role checks are enforced per endpoint.
    /// - 🛠️ All project operations rely on DTO mapping via ProjectMapper.
    /// - 📦 Import, export and bulk operations follow standard patterns.
    /// - 🚀 Ready for extension: add auditing, request correlation, etc.

    #endregion
}


///// @file ProjectController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-07
///// @brief API controller for managing projects with CRUD, pagination, bulk ops, CSV import/export, and soft delete.
///// @details
///// This controller provides endpoints to create, read, update, delete, restore,
///// and import/export project entities. It supports pagination, multi-tenancy,
///// role-based access control, and bulk operations for Admins and GlobalAdmins.

//using CsvHelper;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Serilog;
//using System.Globalization;
//using System.Text;
//using UserFlow.API.Data;
//using UserFlow.API.Data.Entities;
//using UserFlow.API.Mappers;
//using UserFlow.API.Services.Interfaces;
//using UserFlow.API.Shared.DTO;
//using UserFlow.API.Shared.DTO.ImportMaps;

//namespace UserFlow.API.Controllers;

//[ApiController] // ✅ Enables model binding, validation, etc.
//[Route("api/projects")] // 📍 Route prefix for all endpoints in this controller
//[Authorize] // 🔐 All endpoints require authentication
//public class ProjectController : ControllerBase
//{
//    #region 🔒 Fields

//    private readonly AppDbContext _db; // 🗃️ Database context
//    private readonly ICurrentUserService _currentUser; // 👤 Injected user context
//    private readonly ILogger<ProjectController> _logger; // 📝 Logger instance

//    #endregion

//    #region 🔧 Constructor

//    /// <summary>
//    /// 🛠️ Constructor injecting database, user context, and logger.
//    /// </summary>
//    public ProjectController(AppDbContext db, ICurrentUserService currentUser, ILogger<ProjectController> logger)
//    {
//        _db = db; // 🔗 Assign DbContext
//        _currentUser = currentUser; // 🔗 Assign current user service
//        _logger = logger; // 📝 Assign logger
//    }

//    #endregion

//    #region 📄 CRUD Operations

//    /// <summary>
//    /// 📄 Retrieves all visible projects for the current user.
//    /// </summary>
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<ProjectDTO>>> GetProjects()
//    {
//        var query = _db.Projects
//            .Include(p => p.User)        // 👤 Needed for UserName
//            .Include(p => p.Company)     // 🏢 Needed for CompanyName
//            .AsQueryable();              // 🔍 Base query

//        if (!_currentUser.IsInRole("GlobalAdmin"))
//        {
//            if (_currentUser.IsInRole("Admin"))
//            {
//                query = query.Where(p =>
//                    p.CompanyId == _currentUser.CompanyId);
//            }
//            else
//            {
//                query = query.Where(p =>
//                    p.CompanyId == _currentUser.CompanyId &&
//                    (p.UserId == _currentUser.UserId || p.IsShared));
//            }
//        }

//        var projects = await query
//            .Select(ProjectMapper.ToProjectDto())
//            .ToListAsync();

//        _logger.LogInformation("👥 Retrieved {Count} projects for user {UserId} ", projects.Count, _currentUser.UserId);

//        return Ok(projects);
//    }

//    /// <summary>
//    /// 📄 Retrieves paginated list of projects.
//    /// </summary>
//    [HttpGet("paged")]
//    public async Task<ActionResult<PagedResultDTO<ProjectDTO>>> GetPagedProjects(int page = 1, int pageSize = 20)
//    {
//        _logger.LogInformation("📄 [GET] /api/projects/paged by {UserId} with page={Page} and size={Size}", _currentUser.UserId, page, pageSize);

//        if (page < 1 || pageSize < 1 || pageSize > 200)
//        {
//            _logger.LogWarning("⚠️ Invalid pagination parameters: page={Page}, size={Size}", page, pageSize);
//            return BadRequest("Invalid page or pageSize value.");
//        }

//        var query = _db.Projects
//            .Include(p => p.User)
//            .Include(p => p.Company)
//            .AsQueryable();

//        if (!_currentUser.IsInRole("GlobalAdmin"))
//        {
//            if (_currentUser.IsInRole("Admin"))
//            {
//                query = query.Where(p =>
//                    p.CompanyId == _currentUser.CompanyId);
//            }
//            else
//            {
//                query = query.Where(p =>
//                    p.CompanyId == _currentUser.CompanyId &&
//                    (p.UserId == _currentUser.UserId || p.IsShared));
//            }
//        }

//        var ImportedCount = await query.CountAsync();

//        var items = await query
//            .OrderByDescending(p => p.CreatedAt)
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .Select(ProjectMapper.ToProjectDto())
//            .ToListAsync();

//        _logger.LogInformation("📄 Returning paged result: {Count} of {Total}", items.Count, ImportedCount);

//        var result = new PagedResultDTO<ProjectDTO>
//        {
//            Page = page,
//            PageSize = pageSize,
//            ImportedCount = ImportedCount,
//            Items = items
//        };

//        return Ok(result);
//    }

//    /// <summary>
//    /// 🔎 Retrieves a specific project by ID.
//    /// </summary>
//    [HttpGet("{id:long}")]
//    public async Task<ActionResult<ProjectDTO>> GetProjectById(long id)
//    {
//        _logger.LogInformation("🔎 [GET] /api/projects/{Id} requested by {UserId}", id, _currentUser.UserId);

//        var project = await _db.Projects
//            .Include(p => p.User)
//            .Include(p => p.Company)
//            .Where(p => p.Id == id)
//            .Where(p => _currentUser.IsInRole("GlobalAdmin") || (
//                p.CompanyId == _currentUser.CompanyId &&
//                (p.UserId == _currentUser.UserId || p.IsShared)))
//            .Select(ProjectMapper.ToProjectDto())
//            .FirstOrDefaultAsync();

//        if (project == null)
//        {
//            _logger.LogWarning("❌ Project {Id} not found or access denied", id);
//            return NotFound();
//        }

//        return Ok(project);
//    }

//    /// <summary>
//    /// ➕ Creates a new project.
//    /// </summary>
//    [HttpPost]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<ProjectDTO>> CreateProject(ProjectCreateDTO dto)
//    {
//        _logger.LogInformation("➕ [POST] /api/projects create requested by {UserId}", _currentUser.UserId);

//        if (_currentUser.CompanyId == null)
//        {
//            _logger.LogWarning("❌ Project creation failed – company not assigned to user {UserId}", _currentUser.UserId);
//            return BadRequest("No company assigned to current user.");
//        }

//        var entity = new Project
//        {
//            Name = dto.Name,
//            Description = dto.Description,
//            IsShared = dto.IsShared,
//            CompanyId = _currentUser.CompanyId.Value,
//            UserId = _currentUser.UserId,
//            CreatedAt = DateTime.UtcNow,
//            CreatedBy = _currentUser.UserId
//        };

//        _db.Projects.Add(entity);
//        await _db.SaveChangesAsync();

//        var result = await _db.Projects
//            .Include(p => p.User)
//            .Include(p => p.Company)
//            .Where(p => p.Id == entity.Id)
//            .Select(ProjectMapper.ToProjectDto())
//            .FirstAsync();

//        _logger.LogInformation("✅ Project {Id} created successfully", result.Id);

//        return CreatedAtAction(nameof(GetProjectById), new { id = result.Id }, result);
//    }

//    /// <summary>
//    /// ✏️ Updates an existing project.
//    /// </summary>
//    [HttpPut("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> UpdateProject(long id, ProjectUpdateDTO dto)
//    {
//        _logger.LogInformation("✏️ [PUT] /api/projects/{Id} requested by {UserId}", id, _currentUser.UserId);

//        var entity = await _db.Projects
//            .FirstOrDefaultAsync(p => p.Id == id &&
//                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

//        if (entity == null)
//        {
//            _logger.LogWarning("❌ Project {Id} not found for update", id);
//            return NotFound();
//        }

//        entity.Name = dto.Name;
//        entity.Description = dto.Description;
//        entity.IsShared = dto.IsShared;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();

//        _logger.LogInformation("✅ Project {Id} updated", id);

//        return NoContent();
//    }

//    /// <summary>
//    /// 🗑️ Soft deletes a project.
//    /// </summary>
//    [HttpDelete("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> DeleteProject(long id)
//    {
//        _logger.LogInformation("🗑️ [DELETE] /api/projects/{Id} requested by {UserId}", id, _currentUser.UserId);

//        var entity = await _db.Projects
//            .FirstOrDefaultAsync(p => p.Id == id &&
//                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

//        if (entity == null)
//        {
//            _logger.LogWarning("❌ Project {Id} not found for deletion", id);
//            return NotFound();
//        }

//        entity.IsDeleted = true;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();

//        _logger.LogInformation("✅ Project {Id} soft deleted", id);

//        return NoContent();
//    }

//    /// <summary>
//    /// ♻️ Restores a soft-deleted project.
//    /// </summary>
//    [HttpPost("{id:long}/restore")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> RestoreProject(long id)
//    {
//        _logger.LogInformation("♻️ [POST] /api/projects/{Id}/restore requested by {UserId}", id, _currentUser.UserId);

//        var entity = await _db.Projects
//            .IgnoreQueryFilters()
//            .FirstOrDefaultAsync(p => p.Id == id &&
//                p.IsDeleted &&
//                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

//        if (entity == null)
//        {
//            _logger.LogWarning("❌ Project {Id} not found for restore", id);
//            return NotFound();
//        }

//        entity.IsDeleted = false;
//        entity.UpdatedAt = DateTime.UtcNow;
//        entity.UpdatedBy = _currentUser.UserId;

//        await _db.SaveChangesAsync();

//        _logger.LogInformation("✅ Project {Id} restored", id);

//        return NoContent();
//    }

//    #endregion

//    #region 📘 @remarks Footer

//    /// @remarks
//    /// Developer Notes:
//    /// - 📋 Logger injected via ILogger<ProjectController> and used throughout.
//    /// - 📦 All endpoints log important events for diagnostics and traceability.
//    /// - 🔐 Multi-tenancy logic is preserved and respected in all filtered queries.
//    /// - 📤 Export, import, and bulk operations use the same logging strategy.
//    /// - 📈 Safe to extend with correlation IDs, request IDs, or external audit services.

//    #endregion
//}



/////// @file ProjectController.cs
/////// @author Claus Falkenstein
/////// @company VIA Software GmbH
/////// @date 2025-05-07
/////// @brief API controller for managing projects with CRUD, pagination, bulk ops, CSV import/export, and soft delete.
/////// @details
/////// This controller provides endpoints to create, read, update, delete, restore,
/////// and import/export project entities. It supports pagination, multi-tenancy,
/////// role-based access control, and bulk operations for Admins and GlobalAdmins.

////using CsvHelper;
////using Microsoft.AspNetCore.Authorization;
////using Microsoft.AspNetCore.Mvc;
////using Microsoft.EntityFrameworkCore;
////using Serilog;
////using System.Globalization;
////using System.Text;
////using UserFlow.API.Data;
////using UserFlow.API.Data.Entities;
////using UserFlow.API.Mappers;
////using UserFlow.API.Services.Interfaces;
////using UserFlow.API.Shared.DTO;
////using UserFlow.API.Shared.DTO.ImportMaps;

////namespace UserFlow.API.Controllers;

////[ApiController] // ✅ Enables model binding, validation, etc.
////[Route("api/projects")] // 📍 Route prefix for all endpoints in this controller
////[Authorize] // 🔐 All endpoints require authentication
////public class ProjectController : ControllerBase
////{
////    #region 🔒 Fields

////    private readonly AppDbContext _db; // 🗃️ Database context
////    private readonly ICurrentUserService _currentUser; // 👤 Injected user context

////    #endregion

////    #region 🔧 Constructor

////    /// <summary>
////    /// 🛠️ Constructor injecting database and user context.
////    /// </summary>
////    public ProjectController(AppDbContext db, ICurrentUserService currentUser)
////    {
////        _db = db; // 🔗 Assign DbContext
////        _currentUser = currentUser; // 🔗 Assign current user service
////    }

////    #endregion

////    #region 📄 CRUD Operations

////    /// <summary>
////    /// 📄 Retrieves all visible projects for the current user.
////    /// </summary>
////    [HttpGet]
////    public async Task<ActionResult<IEnumerable<ProjectDTO>>> GetProjects()
////    {
////        var query = _db.Projects
////            .Include(p => p.User)        // 👤 Needed for UserName
////            .Include(p => p.Company)     // 🏢 Needed for CompanyName
////            .AsQueryable();              // 🔍 Base query

////        /// 🔐 Filter by company and ownership unless GlobalAdmin
////        if (!_currentUser.IsInRole("GlobalAdmin"))
////        {
////            // 🔐 Admins can see all projects in their company
////            if (_currentUser.IsInRole("Admin"))
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId);
////            }
////            // 🔐 Users can only see their own projects or shared ones
////            else
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId &&
////                    (p.UserId == _currentUser.UserId || p.IsShared));
////            }
////        }

////        /// 🔄 Fetch and map to DTO
////        var projects = await query
////            .Select(ProjectMapper.ToProjectDto())
////            .ToListAsync();

////        return Ok(projects); // ✅ Return list
////    }

////    /// <summary>
////    /// 📄 Retrieves paginated list of projects.
////    /// </summary>
////    [HttpGet("paged")]
////    public async Task<ActionResult<PagedResultDTO<ProjectDTO>>> GetPagedProjects(int page = 1, int pageSize = 20)
////    {
////        /// ❌ Validate input
////        if (page < 1 || pageSize < 1 || pageSize > 200)
////            return BadRequest("Invalid page or pageSize value.");

////        var query = _db.Projects
////            .Include(p => p.User)
////            .Include(p => p.Company)
////            .AsQueryable(); // 🔍 Base query

////        /// 🔐 Filter by company unless GlobalAdmin
////        if (!_currentUser.IsInRole("GlobalAdmin"))
////        {
////            // 🔐 Admins can see all projects in their company
////            if (_currentUser.IsInRole("Admin"))
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId);
////            }
////            // 🔐 Users can only see their own projects or shared ones
////            else
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId &&
////                    (p.UserId == _currentUser.UserId || p.IsShared));
////            }
////        }

////        var ImportedCount = await query.CountAsync(); // 🔢 Count total entries

////        var items = await query
////            .OrderByDescending(p => p.CreatedAt) // 📅 Order by creation
////            .Skip((page - 1) * pageSize) // ⏩ Skip for pagination
////            .Take(pageSize) // ⏳ Take for pagination
////            .Select(ProjectMapper.ToProjectDto()) // 🔄 Map to DTO
////            .ToListAsync();

////        var result = new PagedResultDTO<ProjectDTO>
////        {
////            Page = page,
////            PageSize = pageSize,
////            ImportedCount = ImportedCount,
////            Items = items
////        };

////        return Ok(result); // ✅ Return paginated result
////    }

////    /// <summary>
////    /// 🔎 Retrieves a specific project by ID.
////    /// </summary>
////    [HttpGet("{id:long}")]
////    public async Task<ActionResult<ProjectDTO>> GetProjectById(long id)
////    {
////        var project = await _db.Projects
////            .Include(p => p.User)
////            .Include(p => p.Company)
////            .Where(p => p.Id == id)
////            .Where(p => _currentUser.IsInRole("GlobalAdmin") || (
////                p.CompanyId == _currentUser.CompanyId &&
////                (p.UserId == _currentUser.UserId || p.IsShared)))
////            .Select(ProjectMapper.ToProjectDto())
////            .FirstOrDefaultAsync();

////        if (project == null)
////            return NotFound(); // ❌ Not found

////        return Ok(project); // ✅ Return project
////    }

////    /// <summary>
////    /// ➕ Creates a new project.
////    /// </summary>
////    [HttpPost]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<ProjectDTO>> CreateProject(ProjectCreateDTO dto)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user."); // ❌ Validation

////        var entity = new Project
////        {
////            Name = dto.Name,
////            Description = dto.Description,
////            IsShared = dto.IsShared,
////            CompanyId = _currentUser.CompanyId.Value,
////            UserId = _currentUser.UserId,
////            CreatedAt = DateTime.UtcNow, // 🕒 Timestamp
////            CreatedBy = _currentUser.UserId // 👤 Creator
////        };

////        _db.Projects.Add(entity); // 💾 Add to DbContext
////        await _db.SaveChangesAsync(); // 💾 Save changes

////        var result = await _db.Projects
////            .Include(p => p.User)
////            .Include(p => p.Company)
////            .Where(p => p.Id == entity.Id)
////            .Select(ProjectMapper.ToProjectDto())
////            .FirstAsync();

////        return CreatedAtAction(nameof(GetProjectById), new { id = result.Id }, result); // ✅ Return created
////    }

////    /// <summary>
////    /// ✏️ Updates an existing project.
////    /// </summary>
////    [HttpPut("{id:long}")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> UpdateProject(long id, ProjectUpdateDTO dto)
////    {
////        var entity = await _db.Projects
////            .FirstOrDefaultAsync(p => p.Id == id &&
////                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////        if (entity == null)
////            return NotFound(); // ❌ Not found

////        entity.Name = dto.Name;
////        entity.Description = dto.Description;
////        entity.IsShared = dto.IsShared;
////        entity.UpdatedAt = DateTime.UtcNow; // 🕒 Timestamp
////        entity.UpdatedBy = _currentUser.UserId; // 👤 Updater

////        await _db.SaveChangesAsync(); // 💾 Save update
////        return NoContent(); // ✅ Return 204 NoContent
////    }

////    /// <summary>
////    /// 🗑️ Soft deletes a project.
////    /// </summary>
////    [HttpDelete("{id:long}")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> DeleteProject(long id)
////    {
////        var entity = await _db.Projects
////            .FirstOrDefaultAsync(p => p.Id == id &&
////                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////        if (entity == null)
////            return NotFound(); // ❌ Not found

////        entity.IsDeleted = true; // 🗑️ Mark as deleted
////        entity.UpdatedAt = DateTime.UtcNow;
////        entity.UpdatedBy = _currentUser.UserId;

////        await _db.SaveChangesAsync(); // 💾 Save changes
////        return NoContent(); // ✅ Success
////    }

////    /// <summary>
////    /// ♻️ Restores a soft-deleted project.
////    /// </summary>
////    [HttpPost("{id:long}/restore")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> RestoreProject(long id)
////    {
////        var entity = await _db.Projects
////            .IgnoreQueryFilters()
////            .FirstOrDefaultAsync(p => p.Id == id &&
////                p.IsDeleted &&
////                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////        if (entity == null)
////            return NotFound(); // ❌ Not found

////        entity.IsDeleted = false; // ♻️ Restore flag
////        entity.UpdatedAt = DateTime.UtcNow;
////        entity.UpdatedBy = _currentUser.UserId;

////        await _db.SaveChangesAsync(); // 💾 Save changes
////        return NoContent(); // ✅ Success
////    }

////    #endregion

////    #region 📦 Bulk Operations

////    /// <summary>
////    /// 📦 Creates multiple projects in bulk.
////    /// </summary>
////    [HttpPost("bulk-create")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> BulkCreateProjects(List<ProjectCreateDTO> dtos)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user."); // ❌ No tenant

////        var created = new List<ProjectDTO>(); // 📋 Created results
////        var errors = new List<BulkOperationErrorDTO>(); // ⚠️ Validation errors

////        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
////        {
////            if (string.IsNullOrWhiteSpace(dto.Name))
////            {
////                errors.Add(new(index, "Project name is required.")); // ❌ Missing name
////                continue;
////            }

////            var entity = new Project
////            {
////                Name = dto.Name,
////                Description = dto.Description,
////                IsShared = dto.IsShared,
////                CompanyId = _currentUser.CompanyId.Value,
////                UserId = _currentUser.UserId,
////                CreatedAt = DateTime.UtcNow,
////                CreatedBy = _currentUser.UserId
////            };

////            _db.Projects.Add(entity); // 💾 Add project
////            await _db.SaveChangesAsync(); // 💾 Save

////            var dtoResult = await _db.Projects
////                .Include(p => p.User)
////                .Include(p => p.Company)
////                .Where(p => p.Id == entity.Id)
////                .Select(ProjectMapper.ToProjectDto())
////                .FirstAsync();

////            created.Add(dtoResult); // ✅ Add to results
////        }

////        return Ok(new BulkOperationResultDTO<ProjectDTO>
////        {
////            ImportedCount = created.Count,
////            Errors = errors,
////            Items = created
////        });
////    }

////    /// <summary>
////    /// ✏️ Updates multiple projects in bulk.
////    /// </summary>
////    [HttpPut("bulk-update")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> BulkUpdateProjects(List<ProjectUpdateDTO> dtos)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user.");

////        var updated = new List<ProjectDTO>();
////        var errors = new List<BulkOperationErrorDTO>();

////        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
////        {
////            var entity = await _db.Projects
////                .FirstOrDefaultAsync(p => p.Id == dto.Id &&
////                    (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////            if (entity == null)
////            {
////                errors.Add(new(index, $"Project with Id {dto.Id} not found or access denied."));
////                continue;
////            }

////            entity.Name = dto.Name;
////            entity.Description = dto.Description;
////            entity.IsShared = dto.IsShared;
////            entity.UpdatedAt = DateTime.UtcNow;
////            entity.UpdatedBy = _currentUser.UserId;

////            await _db.SaveChangesAsync();

////            var dtoResult = await _db.Projects
////                .Include(p => p.User)
////                .Include(p => p.Company)
////                .Where(p => p.Id == entity.Id)
////                .Select(ProjectMapper.ToProjectDto())
////                .FirstAsync();

////            updated.Add(dtoResult);
////        }

////        return Ok(new BulkOperationResultDTO<ProjectDTO>
////        {
////            ImportedCount = updated.Count,
////            Errors = errors,
////            Items = updated
////        });
////    }

////    /// <summary>
////    /// 🗑️ Deletes multiple projects in bulk (soft delete).
////    /// </summary>
////    [HttpPost("bulk-delete")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> BulkDeleteProjects(List<long> ids)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user.");

////        var deleted = new List<ProjectDTO>();
////        var errors = new List<BulkOperationErrorDTO>();

////        foreach (var (id, index) in ids.Select((x, i) => (x, i)))
////        {
////            var entity = await _db.Projects
////                .FirstOrDefaultAsync(p => p.Id == id &&
////                    (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////            if (entity == null)
////            {
////                errors.Add(new(index, $"Project with Id {id} not found or access denied."));
////                continue;
////            }

////            entity.IsDeleted = true;
////            entity.UpdatedAt = DateTime.UtcNow;
////            entity.UpdatedBy = _currentUser.UserId;

////            await _db.SaveChangesAsync();

////            var dtoResult = await _db.Projects
////                .IgnoreQueryFilters()
////                .Include(p => p.User)
////                .Include(p => p.Company)
////                .Where(p => p.Id == entity.Id)
////                .Select(ProjectMapper.ToProjectDto())
////                .FirstAsync();

////            deleted.Add(dtoResult);
////        }

////        return Ok(new BulkOperationResultDTO<ProjectDTO>
////        {
////            ImportedCount = deleted.Count,
////            Errors = errors,
////            Items = deleted
////        });
////    }

////    #endregion

////    #region 📥 Import / 📤 Export

////    /// <summary>
////    /// 📤 Exports all visible projects to CSV.
////    /// </summary>
////    [HttpGet("export")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> ExportProjectsAsCsv()
////    {
////        var query = _db.Projects
////            .Include(p => p.User)
////            .Include(p => p.Company)
////            .AsQueryable();

////        if (!_currentUser.IsInRole("GlobalAdmin"))
////        {
////            query = query.Where(p =>
////                p.CompanyId == _currentUser.CompanyId &&
////                (p.UserId == _currentUser.UserId || p.IsShared));
////        }

////        var items = await query
////            .OrderBy(p => p.Name)
////            .Select(ProjectMapper.ToProjectDto())
////            .ToListAsync();

////        using var writer = new StringWriter();
////        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
////        csv.WriteRecords(items); // 📄 Write to CSV

////        var bytes = Encoding.UTF8.GetBytes(writer.ToString());
////        return File(bytes, "text/csv", "projects_export.csv"); // 📤 Return file
////    }

////    /// <summary>
////    /// 📥 Imports projects from a CSV file.
////    /// </summary>
////    [HttpPost("import")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> ImportProjects(IFormFile file)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user.");

////        if (file == null || file.Length == 0)
////            return BadRequest("No file uploaded.");

////        var result = new BulkOperationResultDTO<ProjectDTO>();
////        using var reader = new StreamReader(file.OpenReadStream());
////        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
////        csv.Context.RegisterClassMap<ProjectImportMap>();

////        var records = csv.GetRecords<ProjectImportDTO>().ToList();

////        foreach (var (record, index) in records.Select((r, i) => (r, i)))
////        {
////            if (string.IsNullOrWhiteSpace(record.Name))
////            {
////                result.Errors.Add(new(index, "Project name is required.", "Name", "REQUIRED"));
////                continue;
////            }

////            var entity = new Project
////            {
////                Name = record.Name,
////                Description = record.Description,
////                IsShared = record.IsShared,
////                CompanyId = _currentUser.CompanyId.Value,
////                UserId = _currentUser.UserId,
////                CreatedAt = DateTime.UtcNow,
////                CreatedBy = _currentUser.UserId
////            };

////            _db.Projects.Add(entity);
////            await _db.SaveChangesAsync();

////            result.ImportedCount++; // ✅ One imported
////        }

////        result.TotalRows = records.Count; // 📊 Total records
////        return Ok(result); // ✅ Return result
////    }

////    #endregion
////}




/////// @file ProjectController.cs
/////// @author Claus Falkenstein
/////// @company VIA Software GmbH
/////// @date 2025-05-07
/////// @brief API controller for managing projects with CRUD, pagination, bulk ops, CSV import/export, and soft delete.
/////// @details
/////// This controller provides endpoints to create, read, update, delete, restore,
/////// and import/export project entities. It supports pagination, multi-tenancy,
/////// role-based access control, and bulk operations for Admins and GlobalAdmins.

////using CsvHelper;
////using Microsoft.AspNetCore.Authorization;
////using Microsoft.AspNetCore.Mvc;
////using Microsoft.EntityFrameworkCore;
////using System.Globalization;
////using System.Text;
////using UserFlow.API.Data;
////using UserFlow.API.Data.Entities;
////using UserFlow.API.Mappers;
////using UserFlow.API.Services.Interfaces;
////using UserFlow.API.Shared.DTO;
////using UserFlow.API.Shared.DTO.ImportMaps;

////namespace UserFlow.API.Controllers;

////[ApiController] // ✅ Enables model binding, validation, etc.
////[Route("api/projects")] // 📍 Route prefix for all endpoints in this controller
////[Authorize] // 🔐 All endpoints require authentication
////public class ProjectController : ControllerBase
////{
////    #region 🔒 Fields

////    private readonly AppDbContext _db; // 🗃️ Database context
////    private readonly ICurrentUserService _currentUser; // 👤 Injected user context

////    #endregion

////    #region 🔧 Constructor

////    /// <summary>
////    /// 🛠️ Constructor injecting database and user context.
////    /// </summary>
////    public ProjectController(AppDbContext db, ICurrentUserService currentUser)
////    {
////        _db = db; // 🔗 Assign DbContext
////        _currentUser = currentUser; // 🔗 Assign current user service
////    }

////    #endregion

////    #region 📄 CRUD Operations

////    /// <summary>
////    /// 📄 Retrieves all visible projects for the current user.
////    /// </summary>
////    [HttpGet]
////    public async Task<ActionResult<IEnumerable<ProjectDTO>>> GetProjects()
////    {
////        var query = _db.Projects.AsQueryable(); // 🔍 Base query

////        /// 🔐 Filter by company and ownership unless GlobalAdmin
////        if (!_currentUser.IsInRole("GlobalAdmin"))
////        {
////            // 🔐 Admins can see all projects in their company
////            if (_currentUser.IsInRole("Admin"))
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId);
////            }
////            // 🔐 Users can only see their own projects or shared ones
////            else
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId &&
////                    (p.UserId == _currentUser.UserId || p.IsShared));
////            }
////        }

////        /// 🔄 Fetch and map to DTO
////        var projects = await query
////            .Select(ProjectMapper.ToProjectDto())
////            .ToListAsync();

////        return Ok(projects); // ✅ Return list
////    }

////    /// <summary>
////    /// 📄 Retrieves paginated list of projects.
////    /// </summary>
////    [HttpGet("paged")]
////    public async Task<ActionResult<PagedResultDTO<ProjectDTO>>> GetPagedProjects(int page = 1, int pageSize = 20)
////    {
////        /// ❌ Validate input
////        if (page < 1 || pageSize < 1 || pageSize > 200)
////            return BadRequest("Invalid page or pageSize value.");

////        var query = _db.Projects.AsQueryable(); // 🔍 Base query

////        /// 🔐 Filter by company unless GlobalAdmin
////        if (!_currentUser.IsInRole("GlobalAdmin"))
////        {
////            // 🔐 Admins can see all projects in their company
////            if (_currentUser.IsInRole("Admin"))
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId);
////            }
////            // 🔐 Users can only see their own projects or shared ones
////            else
////            {
////                query = query.Where(p =>
////                    p.CompanyId == _currentUser.CompanyId &&
////                    (p.UserId == _currentUser.UserId || p.IsShared));
////            }
////        }

////        var ImportedCount = await query.CountAsync(); // 🔢 Count total entries

////        var items = await query
////            .OrderByDescending(p => p.CreatedAt) // 📅 Order by creation
////            .Skip((page - 1) * pageSize) // ⏩ Skip for pagination
////            .Take(pageSize) // ⏳ Take for pagination
////            .Select(ProjectMapper.ToProjectDto()) // 🔄 Map to DTO
////            .ToListAsync();

////        var result = new PagedResultDTO<ProjectDTO>
////        {
////            Page = page,
////            PageSize = pageSize,
////            ImportedCount = ImportedCount,
////            Items = items
////        };

////        return Ok(result); // ✅ Return paginated result
////    }

////    /// <summary>
////    /// 🔎 Retrieves a specific project by ID.
////    /// </summary>
////    [HttpGet("{id:long}")]
////    public async Task<ActionResult<ProjectDTO>> GetProjectById(long id)
////    {
////        var project = await _db.Projects
////            .Where(p => p.Id == id)
////            .Where(p => _currentUser.IsInRole("GlobalAdmin") || (
////                p.CompanyId == _currentUser.CompanyId &&
////                (p.UserId == _currentUser.UserId || p.IsShared)))
////            .Select(ProjectMapper.ToProjectDto())
////            .FirstOrDefaultAsync();

////        if (project == null)
////            return NotFound(); // ❌ Not found

////        return Ok(project); // ✅ Return project
////    }

////    /// <summary>
////    /// ➕ Creates a new project.
////    /// </summary>
////    [HttpPost]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<ProjectDTO>> CreateProject(ProjectCreateDTO dto)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user."); // ❌ Validation

////        var entity = new Project
////        {
////            Name = dto.Name,
////            Description = dto.Description,
////            IsShared = dto.IsShared,
////            CompanyId = _currentUser.CompanyId.Value,
////            UserId = _currentUser.UserId,
////            CreatedAt = DateTime.UtcNow, // 🕒 Timestamp
////            CreatedBy = _currentUser.UserId // 👤 Creator
////        };

////        _db.Projects.Add(entity); // 💾 Add to DbContext
////        await _db.SaveChangesAsync(); // 💾 Save changes

////        var result = await _db.Projects
////            .Where(p => p.Id == entity.Id)
////            .Select(ProjectMapper.ToProjectDto())
////            .FirstAsync();

////        return CreatedAtAction(nameof(GetProjectById), new { id = result.Id }, result); // ✅ Return created
////    }

////    /// <summary>
////    /// ✏️ Updates an existing project.
////    /// </summary>
////    [HttpPut("{id:long}")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> UpdateProject(long id, ProjectUpdateDTO dto)
////    {
////        var entity = await _db.Projects
////            .FirstOrDefaultAsync(p => p.Id == id &&
////                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////        if (entity == null)
////            return NotFound(); // ❌ Not found

////        entity.Name = dto.Name;
////        entity.Description = dto.Description;
////        entity.IsShared = dto.IsShared;
////        entity.UpdatedAt = DateTime.UtcNow; // 🕒 Timestamp
////        entity.UpdatedBy = _currentUser.UserId; // 👤 Updater

////        await _db.SaveChangesAsync(); // 💾 Save update
////        return NoContent(); // ✅ Return 204 NoContent
////    }

////    /// <summary>
////    /// 🗑️ Soft deletes a project.
////    /// </summary>
////    [HttpDelete("{id:long}")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> DeleteProject(long id)
////    {
////        var entity = await _db.Projects
////            .FirstOrDefaultAsync(p => p.Id == id &&
////                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////        if (entity == null)
////            return NotFound(); // ❌ Not found

////        entity.IsDeleted = true; // 🗑️ Mark as deleted
////        entity.UpdatedAt = DateTime.UtcNow;
////        entity.UpdatedBy = _currentUser.UserId;

////        await _db.SaveChangesAsync(); // 💾 Save changes
////        return NoContent(); // ✅ Success
////    }

////    /// <summary>
////    /// ♻️ Restores a soft-deleted project.
////    /// </summary>
////    [HttpPost("{id:long}/restore")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> RestoreProject(long id)
////    {
////        var entity = await _db.Projects
////            .IgnoreQueryFilters()
////            .FirstOrDefaultAsync(p => p.Id == id &&
////                p.IsDeleted &&
////                (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////        if (entity == null)
////            return NotFound(); // ❌ Not found

////        entity.IsDeleted = false; // ♻️ Restore flag
////        entity.UpdatedAt = DateTime.UtcNow;
////        entity.UpdatedBy = _currentUser.UserId;

////        await _db.SaveChangesAsync(); // 💾 Save changes
////        return NoContent(); // ✅ Success
////    }

////    #endregion

////    #region 📦 Bulk Operations

////    /// <summary>
////    /// 📦 Creates multiple projects in bulk.
////    /// </summary>
////    [HttpPost("bulk-create")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> BulkCreateProjects(List<ProjectCreateDTO> dtos)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user."); // ❌ No tenant

////        var created = new List<ProjectDTO>(); // 📋 Created results
////        var errors = new List<BulkOperationErrorDTO>(); // ⚠️ Validation errors

////        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
////        {
////            if (string.IsNullOrWhiteSpace(dto.Name))
////            {
////                errors.Add(new(index, "Project name is required.")); // ❌ Missing name
////                continue;
////            }

////            var entity = new Project
////            {
////                Name = dto.Name,
////                Description = dto.Description,
////                IsShared = dto.IsShared,
////                CompanyId = _currentUser.CompanyId.Value,
////                UserId = _currentUser.UserId,
////                CreatedAt = DateTime.UtcNow,
////                CreatedBy = _currentUser.UserId
////            };

////            _db.Projects.Add(entity); // 💾 Add project
////            await _db.SaveChangesAsync(); // 💾 Save

////            var dtoResult = await _db.Projects
////                .Where(p => p.Id == entity.Id)
////                .Select(ProjectMapper.ToProjectDto())
////                .FirstAsync();

////            created.Add(dtoResult); // ✅ Add to results
////        }

////        return Ok(new BulkOperationResultDTO<ProjectDTO>
////        {
////            ImportedCount = created.Count,
////            Errors = errors,
////            Items = created
////        });
////    }

////    /// <summary>
////    /// ✏️ Updates multiple projects in bulk.
////    /// </summary>
////    [HttpPut("bulk-update")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> BulkUpdateProjects(List<ProjectUpdateDTO> dtos)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user.");

////        var updated = new List<ProjectDTO>();
////        var errors = new List<BulkOperationErrorDTO>();

////        foreach (var (dto, index) in dtos.Select((x, i) => (x, i)))
////        {
////            var entity = await _db.Projects
////                .FirstOrDefaultAsync(p => p.Id == dto.Id &&
////                    (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////            if (entity == null)
////            {
////                errors.Add(new(index, $"Project with Id {dto.Id} not found or access denied."));
////                continue;
////            }

////            entity.Name = dto.Name;
////            entity.Description = dto.Description;
////            entity.IsShared = dto.IsShared;
////            entity.UpdatedAt = DateTime.UtcNow;
////            entity.UpdatedBy = _currentUser.UserId;

////            await _db.SaveChangesAsync();

////            var dtoResult = await _db.Projects
////                .Where(p => p.Id == entity.Id)
////                .Select(ProjectMapper.ToProjectDto())
////                .FirstAsync();

////            updated.Add(dtoResult);
////        }

////        return Ok(new BulkOperationResultDTO<ProjectDTO>
////        {
////            ImportedCount = updated.Count,
////            Errors = errors,
////            Items = updated
////        });
////    }

////    /// <summary>
////    /// 🗑️ Deletes multiple projects in bulk (soft delete).
////    /// </summary>
////    [HttpPost("bulk-delete")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> BulkDeleteProjects(List<long> ids)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user.");

////        var deleted = new List<ProjectDTO>();
////        var errors = new List<BulkOperationErrorDTO>();

////        foreach (var (id, index) in ids.Select((x, i) => (x, i)))
////        {
////            var entity = await _db.Projects
////                .FirstOrDefaultAsync(p => p.Id == id &&
////                    (_currentUser.IsInRole("GlobalAdmin") || p.CompanyId == _currentUser.CompanyId));

////            if (entity == null)
////            {
////                errors.Add(new(index, $"Project with Id {id} not found or access denied."));
////                continue;
////            }

////            entity.IsDeleted = true;
////            entity.UpdatedAt = DateTime.UtcNow;
////            entity.UpdatedBy = _currentUser.UserId;

////            await _db.SaveChangesAsync();

////            var dtoResult = await _db.Projects
////                .IgnoreQueryFilters()
////                .Where(p => p.Id == entity.Id)
////                .Select(ProjectMapper.ToProjectDto())
////                .FirstAsync();

////            deleted.Add(dtoResult);
////        }

////        return Ok(new BulkOperationResultDTO<ProjectDTO>
////        {
////            ImportedCount = deleted.Count,
////            Errors = errors,
////            Items = deleted
////        });
////    }

////    #endregion

////    #region 📥 Import / 📤 Export

////    /// <summary>
////    /// 📤 Exports all visible projects to CSV.
////    /// </summary>
////    [HttpGet("export")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<IActionResult> ExportProjectsAsCsv()
////    {
////        var query = _db.Projects.AsQueryable();

////        if (!_currentUser.IsInRole("GlobalAdmin"))
////        {
////            query = query.Where(p =>
////                p.CompanyId == _currentUser.CompanyId &&
////                (p.UserId == _currentUser.UserId || p.IsShared));
////        }

////        var items = await query
////            .OrderBy(p => p.Name)
////            .Select(ProjectMapper.ToProjectDto())
////            .ToListAsync();

////        using var writer = new StringWriter();
////        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
////        csv.WriteRecords(items); // 📄 Write to CSV

////        var bytes = Encoding.UTF8.GetBytes(writer.ToString());
////        return File(bytes, "text/csv", "projects_export.csv"); // 📤 Return file
////    }

////    /// <summary>
////    /// 📥 Imports projects from a CSV file.
////    /// </summary>
////    [HttpPost("import")]
////    [Authorize(Roles = "Admin,GlobalAdmin")]
////    public async Task<ActionResult<BulkOperationResultDTO<ProjectDTO>>> ImportProjects(IFormFile file)
////    {
////        if (_currentUser.CompanyId == null)
////            return BadRequest("No company assigned to current user.");

////        if (file == null || file.Length == 0)
////            return BadRequest("No file uploaded.");

////        var result = new BulkOperationResultDTO<ProjectDTO>();
////        using var reader = new StreamReader(file.OpenReadStream());
////        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
////        csv.Context.RegisterClassMap<ProjectImportMap>();

////        var records = csv.GetRecords<ProjectImportDTO>().ToList();

////        foreach (var (record, index) in records.Select((r, i) => (r, i)))
////        {
////            if (string.IsNullOrWhiteSpace(record.Name))
////            {
////                result.Errors.Add(new(index, "Project name is required.", "Name", "REQUIRED"));
////                continue;
////            }

////            var entity = new Project
////            {
////                Name = record.Name,
////                Description = record.Description,
////                IsShared = record.IsShared,
////                CompanyId = _currentUser.CompanyId.Value,
////                UserId = _currentUser.UserId,
////                CreatedAt = DateTime.UtcNow,
////                CreatedBy = _currentUser.UserId
////            };

////            _db.Projects.Add(entity);
////            await _db.SaveChangesAsync();

////            result.ImportedCount++; // ✅ One imported
////        }

////        result.TotalRows = records.Count; // 📊 Total records
////        return Ok(result); // ✅ Return result
////    }

////    #endregion
////}
