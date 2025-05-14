///// @file EmployeeController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-07
///// @brief Provides CRUD and bulk operations for managing employees.
///// @details
///// This controller allows Admins and GlobalAdmins to manage employee records.
///// Supports listing, detail view, creation, update, soft delete, restore,
///// bulk operations, and CSV import/export functionality.
/////
///// @endpoints
///// - GET /api/employees → List all employees (paged, with optional company include)
///// - GET /api/employees/{id} → Get single employee by ID
///// - POST /api/employees → Create new employee
///// - PUT /api/employees → Update employee
///// - DELETE /api/employees/{id} → Soft delete employee
///// - PUT /api/employees/{id}/restore → Restore soft-deleted employee
///// - POST /api/employees/bulk → Bulk create employees
///// - PUT /api/employees/bulk → Bulk update employees
///// - POST /api/employees/bulk-delete → Bulk soft-delete employees
///// - GET /api/employees/export → Export employees as CSV
///// - POST /api/employees/import → Import employees from CSV

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data;
using UserFlow.API.Data.Entities;
using UserFlow.API.Mappers;
using UserFlow.API.Services.Interfaces;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "Admin,GlobalAdmin")]
public class EmployeeController : ControllerBase
{
    #region 🔒 Private Fields

    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<EmployeeController> _logger;
    private readonly ICurrentUserService _currentUser;

    #endregion

    #region 🔧 Constructor

    public EmployeeController(
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<EmployeeController> logger,
        ICurrentUserService currentUser)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _currentUser = currentUser;
    }

    #endregion

    #region 📃 Helper Methods

    private async Task<Employee?> GetCurrentEmployeeAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user != null
            ? await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == user.Id)
            : null;
    }

    private bool IsGlobalAdmin() => User.IsInRole("GlobalAdmin");

    private async Task<long?> GetAuthorizedCompanyIdAsync()
    {
        if (IsGlobalAdmin()) return null;
        var employee = await GetCurrentEmployeeAsync();
        return employee?.CompanyId;
    }

    private async Task<bool> ValidateUserIdAsync(long userId)
    {
        return await _userManager.Users
            .AnyAsync(u => u.Id == userId);
    }

    private async Task<(List<Employee> Valid, List<BulkOperationErrorDTO> Errors)> ValidateBulkEmployeesAsync(
        List<EmployeeCreateDTO> dtos,
        long companyId)
    {
        var errors = new List<BulkOperationErrorDTO>();
        var validEmployees = new List<Employee>();
        var existingEmails = new HashSet<string>();
        var existingUserIds = new HashSet<long>();

        foreach (var (dto, index) in dtos.Select((d, i) => (d, i + 1)))
        {
            var error = new BulkOperationErrorDTO { RecordIndex = index };

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                error.Field = nameof(dto.Email);
                error.Code = "EmailRequired";
                error.Message = "Email is required";
                errors.Add(error);
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                error.Field = nameof(dto.Name);
                error.Code = "NameRequired";
                error.Message = "Name is required";
                errors.Add(error);
            }

            if (dto.UserId <= 0)
            {
                error.Field = nameof(dto.UserId);
                error.Code = "InvalidUserId";
                error.Message = "Invalid User ID";
                errors.Add(error);
            }

            if (existingEmails.Contains(dto.Email))
            {
                error.Field = nameof(dto.Email);
                error.Code = "DuplicateInBatch";
                error.Message = "Duplicate email in batch";
                errors.Add(error);
            }

            if (existingUserIds.Contains(dto.UserId))
            {
                error.Field = nameof(dto.UserId);
                error.Code = "DuplicateInBatch";
                error.Message = "Duplicate User ID in batch";
                errors.Add(error);
            }

            existingEmails.Add(dto.Email);
            existingUserIds.Add(dto.UserId);

            if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
            {
                error.Field = nameof(dto.Email);
                error.Code = "DuplicateEmail";
                error.Message = "Email already exists";
                errors.Add(error);
            }

            if (await _context.Employees.AnyAsync(e => e.UserId == dto.UserId))
            {
                error.Field = nameof(dto.UserId);
                error.Code = "DuplicateUserId";
                error.Message = "User ID already exists";
                errors.Add(error);
            }

            if (errors.Any(e => e.RecordIndex == index)) continue;

            validEmployees.Add(new Employee
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Role = dto.Role.Trim(),
                CompanyId = companyId,
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        return (validEmployees, errors);
    }

    #endregion

    #region 📄 CRUD Operations

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDTO>>> GetAllAsync(
        [FromQuery] bool includeCompany = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("➡️ GetEmployees called (Page: {Page}, Size: {Size}, IncludeCompany: {IncludeCompany})", page, pageSize, includeCompany);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");
        var query = _context.Employees.AsNoTracking();

        if (!isGlobalAdmin)
        {
            if (_currentUser.CompanyId == null)
            {
                _logger.LogWarning("❌ Access denied for user {UserId} – CompanyId is null", _currentUser.UserId);
                return Forbid("❌ Company-restricted admin without company.");
            }

            query = query.Where(e => e.CompanyId == _currentUser.CompanyId);
        }

        if (includeCompany)
            query = query.Include(e => e.Company);

        var totalCount = await query.CountAsync();
        var employees = await query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(EmployeeMapper.ToEmployeeDto(includeCompany))
            .ToListAsync();

        _logger.LogInformation("✅ GetEmployees returned {Count} employees", employees.Count);

        return Ok(new
        {
            Total = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = employees
        });
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<EmployeeDTO>> GetByIdAsync(long id, [FromQuery] bool includeCompany = false)
    {
        _logger.LogInformation("🔎 GetEmployeeById called for Id {Id}", id);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");
        var query = _context.Employees.AsNoTracking().Where(e => e.Id == id);

        if (!isGlobalAdmin)
        {
            if (_currentUser.CompanyId == null)
            {
                _logger.LogWarning("❌ Access denied for GetEmployeeById – CompanyId is null");
                return Forbid("❌ Company-restricted admin without company.");
            }

            query = query.Where(e => e.CompanyId == _currentUser.CompanyId);
        }

        if (includeCompany)
            query = query.Include(e => e.Company);

        var employee = await query
            .Select(EmployeeMapper.ToEmployeeDto(includeCompany))
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            _logger.LogWarning("❌ Employee with ID {Id} not found", id);
            return NotFound();
        }

        _logger.LogInformation("✅ Employee with ID {Id} retrieved successfully", id);
        return Ok(employee);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] EmployeeCreateDTO dto)
    {
        _logger.LogInformation("➕ CreateEmployee called for Email {Email}", dto.Email);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

        if (!isGlobalAdmin && _currentUser.CompanyId == null)
        {
            _logger.LogWarning("❌ CreateEmployee forbidden – CompanyId is null");
            return Forbid("❌ Company-restricted admin without company.");
        }

        var employee = new Employee
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            Role = dto.Role,
            CompanyId = isGlobalAdmin ? dto.CompanyId : _currentUser.CompanyId,
            UserId = dto.UserId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var result = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employee.Id)
            .Select(EmployeeMapper.ToEmployeeDto())
            .FirstAsync();

        _logger.LogInformation("👤 Created employee '{Name}' ({Email}) in company {CompanyId} by user {UserId}",
            employee.Name, employee.Email, employee.CompanyId, _currentUser.UserId);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = employee.Id }, result);
    }

    #endregion

    #region ✏️ Update / 🗑️ Delete / ♻️ Restore

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] EmployeeUpdateDTO dto)
    {
        _logger.LogInformation("✏️ UpdateEmployee called for Id {Id}", dto.Id);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id);
        if (employee == null)
        {
            _logger.LogWarning("❌ UpdateEmployee failed – Employee with Id {Id} not found", dto.Id);
            return NotFound();
        }

        if (!isGlobalAdmin)
        {
            if (_currentUser.CompanyId == null || employee.CompanyId != _currentUser.CompanyId)
            {
                _logger.LogWarning("❌ UpdateEmployee forbidden – CompanyId mismatch or null");
                return Forbid("❌ You are not allowed to update employees from other companies.");
            }
        }

        employee.Name = dto.Name.Trim();
        employee.Email = dto.Email.Trim();
        employee.Role = dto.Role;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        var result = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == dto.Id)
            .Select(EmployeeMapper.ToEmployeeDto())
            .FirstAsync();

        _logger.LogInformation("✅ Employee '{Name}' ({Id}) updated successfully by user {UserId}", employee.Name, employee.Id, _currentUser.UserId);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        _logger.LogInformation("🗑️ DeleteEmployee called for Id {Id}", id);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
        {
            _logger.LogWarning("❌ DeleteEmployee failed – Employee with Id {Id} not found", id);
            return NotFound();
        }

        if (!isGlobalAdmin)
        {
            if (_currentUser.CompanyId == null || employee.CompanyId != _currentUser.CompanyId)
            {
                _logger.LogWarning("❌ DeleteEmployee forbidden – CompanyId mismatch or null");
                return Forbid("❌ You are not allowed to delete employees from other companies.");
            }
        }

        if (employee.IsDeleted)
        {
            _logger.LogWarning("⚠️ Employee with Id {Id} is already deleted", id);
            return BadRequest("⚠️ Employee is already deleted.");
        }

        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        _logger.LogWarning("🗑️ Employee '{Name}' ({Id}) soft-deleted by user {UserId}", employee.Name, employee.Id, _currentUser.UserId);
        return Ok(new { message = "✅ Employee soft-deleted." });
    }

    [HttpPut("{id:long}/restore")]
    public async Task<IActionResult> RestoreAsync(long id)
    {
        _logger.LogInformation("♻️ RestoreEmployee called for Id {Id}", id);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

        var employee = await _context.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            _logger.LogWarning("❌ RestoreEmployee failed – Employee with Id {Id} not found", id);
            return NotFound();
        }

        if (!isGlobalAdmin)
        {
            if (_currentUser.CompanyId == null || employee.CompanyId != _currentUser.CompanyId)
            {
                _logger.LogWarning("❌ RestoreEmployee forbidden – CompanyId mismatch or null");
                return Forbid("❌ You are not allowed to restore employees from other companies.");
            }
        }

        if (!employee.IsDeleted)
        {
            _logger.LogInformation("ℹ️ Employee with Id {Id} is not deleted – nothing to restore", id);
            return BadRequest("ℹ️ Employee is not deleted.");
        }

        employee.IsDeleted = false;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("♻️ Employee '{Name}' ({Id}) restored by user {UserId}", employee.Name, employee.Id, _currentUser.UserId);
        return Ok(new { message = "✅ Employee restored." });
    }

    #endregion

    #region 📦 Bulk Create / Update / Delete

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreateAsync([FromBody] List<EmployeeCreateDTO> list)
    {
        _logger.LogInformation("📦 BulkCreateEmployees called with {Count} records", list.Count);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");
        var currentCompanyId = _currentUser.CompanyId;

        var result = new BulkOperationResultDTO<EmployeeDTO>();
        var valid = new List<Employee>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (dto, index) in list.Select((dto, i) => (dto, i + 1)))
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                errors.Add(new BulkOperationErrorDTO
                {
                    RecordIndex = index,
                    Field = nameof(dto.Email),
                    Code = "EmailRequired",
                    Message = "E-Mail ist erforderlich."
                });
                continue;
            }

            var email = dto.Email.Trim();

            if (await _context.Employees.AnyAsync(e => e.Email == email))
            {
                errors.Add(new BulkOperationErrorDTO
                {
                    RecordIndex = index,
                    Field = nameof(dto.Email),
                    Code = "DuplicateEmail",
                    Message = $"E-Mail {email} ist bereits vorhanden."
                });
                continue;
            }

            valid.Add(new Employee
            {
                Name = dto.Name.Trim(),
                Email = email,
                Role = dto.Role,
                CompanyId = isGlobalAdmin ? dto.CompanyId : currentCompanyId,
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            });
        }

        if (valid.Any())
        {
            _context.Employees.AddRange(valid);
            await _context.SaveChangesAsync();
        }

        result.TotalRows = list.Count;
        result.ImportedCount = valid.Count;
        result.Errors = errors;

        _logger.LogInformation("✅ BulkCreateEmployees imported {Count} records successfully", valid.Count);
        return Accepted(result);
    }

    [HttpPut("bulk")]
    public async Task<IActionResult> BulkUpdateAsync([FromBody] List<EmployeeUpdateDTO> list)
    {
        _logger.LogInformation("✏️ BulkUpdateEmployees called with {Count} records", list.Count);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");
        var result = new BulkOperationResultDTO<EmployeeDTO>();
        var errors = new List<BulkOperationErrorDTO>();
        var updatedCount = 0;

        foreach (var (dto, index) in list.Select((dto, i) => (dto, i + 1)))
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id);
            if (employee == null)
            {
                errors.Add(new BulkOperationErrorDTO
                {
                    RecordIndex = index,
                    Field = nameof(dto.Id),
                    Code = "NotFound",
                    Message = $"Mitarbeiter mit ID {dto.Id} wurde nicht gefunden."
                });
                continue;
            }

            if (!isGlobalAdmin && employee.CompanyId != _currentUser.CompanyId)
            {
                errors.Add(new BulkOperationErrorDTO
                {
                    RecordIndex = index,
                    Field = nameof(dto.CompanyId),
                    Code = "Unauthorized",
                    Message = $"Zugriff auf Mitarbeiter {dto.Id} nicht erlaubt."
                });
                continue;
            }

            employee.Name = dto.Name.Trim();
            employee.Email = dto.Email.Trim();
            employee.Role = dto.Role;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.UpdatedBy = _currentUser.UserId;

            updatedCount++;
        }

        await _context.SaveChangesAsync();

        result.TotalRows = list.Count;
        result.ImportedCount = updatedCount;
        result.Errors = errors;

        _logger.LogInformation("✅ BulkUpdateEmployees updated {Count} records successfully", updatedCount);
        return Ok(result);
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDeleteAsync([FromBody] long[] ids)
    {
        _logger.LogInformation("🗑️ BulkDeleteEmployees called with {Count} ids", ids.Length);

        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

        var employees = await _context.Employees
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        var errors = new List<BulkOperationErrorDTO>();
        var deletedCount = 0;

        foreach (var employee in employees)
        {
            if (!isGlobalAdmin && employee.CompanyId != _currentUser.CompanyId)
            {
                errors.Add(new BulkOperationErrorDTO
                {
                    RecordIndex = -1,
                    Field = "CompanyId",
                    Code = "Unauthorized",
                    Message = $"Zugriff auf Mitarbeiter {employee.Id} nicht erlaubt."
                });
                continue;
            }

            if (employee.IsDeleted)
                continue;

            employee.IsDeleted = true;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.UpdatedBy = _currentUser.UserId;
            deletedCount++;
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning("🗑️ BulkDeleteEmployees soft-deleted {Count} records", deletedCount);

        return Ok(new BulkOperationResultDTO<EmployeeDTO>
        {
            TotalRows = ids.Length,
            ImportedCount = deletedCount,
            Errors = errors
        });
    }

    #endregion

}


//[ApiController] // ✅ Enables model validation and routing
//[Route("api/employees")] // 📍 Base route for employee endpoints
//[Authorize(Roles = "Admin,GlobalAdmin")] // 🔐 Restrict to Admins and GlobalAdmins
//public class EmployeeController : ControllerBase
//{
//    #region 🔒 Private Fields

//    private readonly AppDbContext _context; // 🧱 EF Core context
//    private readonly UserManager<User> _userManager; // 👤 Identity manager for users
//    private readonly ILogger<EmployeeController> _logger; // 📝 Logging
//    private readonly ICurrentUserService _currentUser; // 👤 Info about current user

//    #endregion

//    #region 🔧 Constructor

//    /// <summary>
//    /// Constructor injecting all required services.
//    /// </summary>
//    public EmployeeController(
//        AppDbContext context,
//        UserManager<User> userManager,
//        ILogger<EmployeeController> logger,
//        ICurrentUserService currentUser)
//    {
//        _context = context;         // 💉 Injected DB context
//        _userManager = userManager; // 💉 Injected identity manager
//        _logger = logger;           // 💉 Injected logger
//        _currentUser = currentUser; // 💉 Injected current user context
//    }

//    #endregion

//    #region 📃 Helper Methods

//    /// <summary>
//    /// 🔍 Gets the currently logged-in employee by UserId.
//    /// </summary>
//    private async Task<Employee?> GetCurrentEmployeeAsync()
//    {
//        var user = await _userManager.GetUserAsync(User); // 👤 Get current user
//        return user != null
//            ? await _context.Employees
//                .AsNoTracking()
//                .FirstOrDefaultAsync(e => e.UserId == user.Id) // 🔗 Match employee by UserId
//            : null;
//    }

//    /// <summary>
//    /// 🔐 Checks if current user has GlobalAdmin role.
//    /// </summary>
//    private bool IsGlobalAdmin() => User.IsInRole("GlobalAdmin");

//    /// <summary>
//    /// 🏢 Gets the current company ID if restricted.
//    /// </summary>
//    private async Task<long?> GetAuthorizedCompanyIdAsync()
//    {
//        if (IsGlobalAdmin()) return null; // ✅ GlobalAdmins are not restricted
//        var employee = await GetCurrentEmployeeAsync(); // 👥 Get current employee
//        return employee?.CompanyId; // 📦 Return company ID
//    }

//    /// <summary>
//    /// ✅ Validates whether a given userId exists.
//    /// </summary>
//    private async Task<bool> ValidateUserIdAsync(long userId)
//    {
//        return await _userManager.Users
//            .AnyAsync(u => u.Id == userId); // 🔍 Check existence
//    }

//    /// <summary>
//    /// 📦 Validates a list of EmployeeCreateDTOs for bulk insert.
//    /// </summary>
//    private async Task<(List<Employee> Valid, List<BulkOperationErrorDTO> Errors)> ValidateBulkEmployeesAsync(
//        List<EmployeeCreateDTO> dtos,
//        long companyId)
//    {
//        var errors = new List<BulkOperationErrorDTO>(); // ❌ Validation errors
//        var validEmployees = new List<Employee>();      // ✅ Valid records
//        var existingEmails = new HashSet<string>();     // 🛡️ Duplicate protection
//        var existingUserIds = new HashSet<long>();

//        foreach (var (dto, index) in dtos.Select((d, i) => (d, i + 1)))
//        {
//            var error = new BulkOperationErrorDTO { RecordIndex = index };

//            /// ❌ Email required
//            if (string.IsNullOrWhiteSpace(dto.Email))
//            {
//                error.Field = nameof(dto.Email);
//                error.Code = "EmailRequired";
//                error.Message = "Email is required";
//                errors.Add(error);
//            }

//            /// ❌ Name required
//            if (string.IsNullOrWhiteSpace(dto.Name))
//            {
//                error.Field = nameof(dto.Name);
//                error.Code = "NameRequired";
//                error.Message = "Name is required";
//                errors.Add(error);
//            }

//            /// ❌ UserId must be valid
//            if (dto.UserId <= 0)
//            {
//                error.Field = nameof(dto.UserId);
//                error.Code = "InvalidUserId";
//                error.Message = "Invalid User ID";
//                errors.Add(error);
//            }

//            /// ❌ Duplicate email in same batch
//            if (existingEmails.Contains(dto.Email))
//            {
//                error.Field = nameof(dto.Email);
//                error.Code = "DuplicateInBatch";
//                error.Message = "Duplicate email in batch";
//                errors.Add(error);
//            }

//            /// ❌ Duplicate UserId in batch
//            if (existingUserIds.Contains(dto.UserId))
//            {
//                error.Field = nameof(dto.UserId);
//                error.Code = "DuplicateInBatch";
//                error.Message = "Duplicate User ID in batch";
//                errors.Add(error);
//            }

//            existingEmails.Add(dto.Email);     // 📌 Track email
//            existingUserIds.Add(dto.UserId);   // 📌 Track userId

//            /// ❌ Email already in DB
//            if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
//            {
//                error.Field = nameof(dto.Email);
//                error.Code = "DuplicateEmail";
//                error.Message = "Email already exists";
//                errors.Add(error);
//            }

//            /// ❌ UserId already in DB
//            if (await _context.Employees.AnyAsync(e => e.UserId == dto.UserId))
//            {
//                error.Field = nameof(dto.UserId);
//                error.Code = "DuplicateUserId";
//                error.Message = "User ID already exists";
//                errors.Add(error);
//            }

//            /// ❌ Skip if any error for this record
//            if (errors.Any(e => e.RecordIndex == index)) continue;

//            /// ✅ Add valid employee
//            validEmployees.Add(new Employee
//            {
//                Name = dto.Name.Trim(),
//                Email = dto.Email.Trim().ToLowerInvariant(),
//                Role = dto.Role.Trim(),
//                CompanyId = companyId,
//                UserId = dto.UserId,
//                CreatedAt = DateTime.UtcNow
//            });
//        }

//        return (validEmployees, errors);
//    }

//    #endregion

//    #region 📄 CRUD Operations

//    /// <summary>
//    /// 📄 Lists all employees (paged, optionally including company).
//    /// </summary>
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<EmployeeDTO>>> GetEmployees(
//        [FromQuery] bool includeCompany = false,
//        [FromQuery] int page = 1,
//        [FromQuery] int pageSize = 20)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        var query = _context.Employees.AsNoTracking();

//        /// 🔐 Restrict to own company if not GlobalAdmin
//        if (!isGlobalAdmin)
//        {
//            if (_currentUser.CompanyId == null)
//                return Forbid("❌ Company-restricted admin without company.");

//            query = query.Where(e => e.CompanyId == _currentUser.CompanyId);
//        }

//        /// ➕ Include company data if requested
//        if (includeCompany)
//            query = query.Include(e => e.Company);

//        /// 🔢 Total record count for pagination
//        var totalCount = await query.CountAsync();

//        /// 📥 Load paged result set
//        var employees = await query
//            .OrderBy(e => e.Name)
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .Select(EmployeeMapper.ToEmployeeDto(includeCompany))
//            .ToListAsync();

//        return Ok(new
//        {
//            Total = totalCount,
//            Page = page,
//            PageSize = pageSize,
//            Items = employees
//        });
//    }

//    /// <summary>
//    /// 🔎 Gets a specific employee by ID.
//    /// </summary>
//    [HttpGet("{id:long}")]
//    public async Task<ActionResult<EmployeeDTO>> GetEmployeeById(long id, [FromQuery] bool includeCompany = false)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        /// 🧱 Build query
//        var query = _context.Employees.AsNoTracking().Where(e => e.Id == id);

//        if (!isGlobalAdmin)
//        {
//            if (_currentUser.CompanyId == null)
//                return Forbid("❌ Company-restricted admin without company.");

//            query = query.Where(e => e.CompanyId == _currentUser.CompanyId);
//        }

//        if (includeCompany)
//            query = query.Include(e => e.Company);

//        var employee = await query
//            .Select(EmployeeMapper.ToEmployeeDto(includeCompany))
//            .FirstOrDefaultAsync();

//        if (employee == null)
//            return NotFound();

//        return Ok(employee);
//    }

//    /// <summary>
//    /// ➕ Creates a new employee.
//    /// </summary>
//    [HttpPost]
//    public async Task<IActionResult> CreateEmployee([FromBody] EmployeeCreateDTO dto)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        if (!isGlobalAdmin && _currentUser.CompanyId == null)
//            return Forbid("❌ Company-restricted admin without company.");

//        /// 🧱 Build entity
//        var employee = new Employee
//        {
//            Name = dto.Name.Trim(),
//            Email = dto.Email.Trim(),
//            Role = dto.Role,
//            CompanyId = isGlobalAdmin ? dto.CompanyId : _currentUser.CompanyId,
//            UserId = dto.UserId,
//            CreatedAt = DateTime.UtcNow,
//            CreatedBy = _currentUser.UserId
//        };

//        /// 💾 Save to DB
//        _context.Employees.Add(employee);
//        await _context.SaveChangesAsync();

//        /// 🔄 Load result as DTO
//        var result = await _context.Employees
//            .AsNoTracking()
//            .Where(e => e.Id == employee.Id)
//            .Select(EmployeeMapper.ToEmployeeDto())
//            .FirstAsync();

//        _logger.LogInformation("👤 Created employee '{Name}' ({Email}) in company {CompanyId} by user {UserId}",
//            employee.Name, employee.Email, employee.CompanyId, _currentUser.UserId);

//        return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id }, result);
//    }

//    #endregion

//    #region ✏️ Update / 🗑️ Delete / ♻️ Restore

//    /// <summary>
//    /// ✏️ Updates an existing employee.
//    /// </summary>
//    [HttpPut]
//    public async Task<IActionResult> UpdateEmployee([FromBody] EmployeeUpdateDTO dto)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id);
//        if (employee == null)
//            return NotFound();

//        /// 🔐 Restrict to own company if not GlobalAdmin
//        if (!isGlobalAdmin)
//        {
//            if (_currentUser.CompanyId == null || employee.CompanyId != _currentUser.CompanyId)
//                return Forbid("❌ You are not allowed to update employees from other companies.");
//        }

//        /// 🧾 Update fields
//        employee.Name = dto.Name.Trim();
//        employee.Email = dto.Email.Trim();
//        employee.Role = dto.Role;
//        employee.UpdatedAt = DateTime.UtcNow;
//        employee.UpdatedBy = _currentUser.UserId;

//        await _context.SaveChangesAsync();

//        var result = await _context.Employees
//            .AsNoTracking()
//            .Where(e => e.Id == dto.Id)
//            .Select(EmployeeMapper.ToEmployeeDto())
//            .FirstAsync();

//        _logger.LogInformation("✏️ Updated employee '{Name}' ({Id}) by user {UserId}",
//            employee.Name, employee.Id, _currentUser.UserId);

//        return Ok(result);
//    }

//    /// <summary>
//    /// 🗑️ Soft-deletes an employee.
//    /// </summary>
//    [HttpDelete("{id:long}")]
//    public async Task<IActionResult> DeleteEmployee(long id)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
//        if (employee == null)
//            return NotFound();

//        /// 🔐 Restrict deletion to company
//        if (!isGlobalAdmin)
//        {
//            if (_currentUser.CompanyId == null || employee.CompanyId != _currentUser.CompanyId)
//                return Forbid("❌ You are not allowed to delete employees from other companies.");
//        }

//        if (employee.IsDeleted)
//            return BadRequest("⚠️ Employee is already deleted.");

//        employee.IsDeleted = true;
//        employee.UpdatedAt = DateTime.UtcNow;
//        employee.UpdatedBy = _currentUser.UserId;

//        await _context.SaveChangesAsync();

//        _logger.LogWarning("🗑️ Soft-deleted employee '{Name}' ({Id}) by user {UserId}",
//            employee.Name, employee.Id, _currentUser.UserId);

//        return Ok(new { message = "✅ Employee soft-deleted." });
//    }

//    /// <summary>
//    /// ♻️ Restores a soft-deleted employee.
//    /// </summary>
//    [HttpPut("{id:long}/restore")]
//    public async Task<IActionResult> RestoreEmployee(long id)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        var employee = await _context.Employees
//            .IgnoreQueryFilters()
//            .FirstOrDefaultAsync(e => e.Id == id);

//        if (employee == null)
//            return NotFound();

//        /// 🔐 Only same company allowed to restore
//        if (!isGlobalAdmin)
//        {
//            if (_currentUser.CompanyId == null || employee.CompanyId != _currentUser.CompanyId)
//                return Forbid("❌ You are not allowed to restore employees from other companies.");
//        }

//        if (!employee.IsDeleted)
//            return BadRequest("ℹ️ Employee is not deleted.");

//        employee.IsDeleted = false;
//        employee.UpdatedAt = DateTime.UtcNow;
//        employee.UpdatedBy = _currentUser.UserId;

//        await _context.SaveChangesAsync();

//        _logger.LogInformation("♻️ Restored employee '{Name}' ({Id}) by user {UserId}",
//            employee.Name, employee.Id, _currentUser.UserId);

//        return Ok(new { message = "✅ Employee restored." });
//    }

//    #endregion

//    #region 📦 Bulk Create / Update / Delete

//    /// <summary>
//    /// 📦 Bulk-creates employees.
//    /// </summary>
//    [HttpPost("bulk")]
//    public async Task<IActionResult> BulkCreateEmployees([FromBody] List<EmployeeCreateDTO> list)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");
//        var currentCompanyId = _currentUser.CompanyId;

//        var result = new BulkOperationResultDTO<EmployeeDTO>();
//        var valid = new List<Employee>();
//        var errors = new List<BulkOperationErrorDTO>();

//        foreach (var (dto, index) in list.Select((dto, i) => (dto, i + 1)))
//        {
//            if (string.IsNullOrWhiteSpace(dto.Email))
//            {
//                errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = index,
//                    Field = nameof(dto.Email),
//                    Code = "EmailRequired",
//                    Message = "E-Mail ist erforderlich."
//                });
//                continue;
//            }

//            var email = dto.Email.Trim();

//            if (await _context.Employees.AnyAsync(e => e.Email == email))
//            {
//                errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = index,
//                    Field = nameof(dto.Email),
//                    Code = "DuplicateEmail",
//                    Message = $"E-Mail {email} ist bereits vorhanden."
//                });
//                continue;
//            }

//            valid.Add(new Employee
//            {
//                Name = dto.Name.Trim(),
//                Email = email,
//                Role = dto.Role,
//                CompanyId = isGlobalAdmin ? dto.CompanyId : currentCompanyId,
//                UserId = dto.UserId,
//                CreatedAt = DateTime.UtcNow,
//                CreatedBy = _currentUser.UserId
//            });
//        }

//        if (valid.Any())
//        {
//            _context.Employees.AddRange(valid);
//            await _context.SaveChangesAsync();
//        }

//        result.TotalRows = list.Count;
//        result.ImportedCount = valid.Count;
//        result.Errors = errors;

//        _logger.LogInformation("📦 Bulk-created {Count} employees by user {UserId}", valid.Count, _currentUser.UserId);
//        return Accepted(result);
//    }

//    /// <summary>
//    /// ✏️ Bulk-updates employees.
//    /// </summary>
//    [HttpPut("bulk")]
//    public async Task<IActionResult> BulkUpdateEmployees([FromBody] List<EmployeeUpdateDTO> list)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");
//        var result = new BulkOperationResultDTO<EmployeeDTO>();
//        var errors = new List<BulkOperationErrorDTO>();
//        var updatedCount = 0;

//        foreach (var (dto, index) in list.Select((dto, i) => (dto, i + 1)))
//        {
//            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id);
//            if (employee == null)
//            {
//                errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = index,
//                    Field = nameof(dto.Id),
//                    Code = "NotFound",
//                    Message = $"Mitarbeiter mit ID {dto.Id} wurde nicht gefunden."
//                });
//                continue;
//            }

//            if (!isGlobalAdmin && employee.CompanyId != _currentUser.CompanyId)
//            {
//                errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = index,
//                    Field = nameof(dto.CompanyId),
//                    Code = "Unauthorized",
//                    Message = $"Zugriff auf Mitarbeiter {dto.Id} nicht erlaubt."
//                });
//                continue;
//            }

//            employee.Name = dto.Name.Trim();
//            employee.Email = dto.Email.Trim();
//            employee.Role = dto.Role;
//            employee.UpdatedAt = DateTime.UtcNow;
//            employee.UpdatedBy = _currentUser.UserId;

//            updatedCount++;
//        }

//        await _context.SaveChangesAsync();

//        result.TotalRows = list.Count;
//        result.ImportedCount = updatedCount;
//        result.Errors = errors;

//        _logger.LogInformation("✏️ Bulk-updated {Count} employees by user {UserId}", updatedCount, _currentUser.UserId);
//        return Ok(result);
//    }

//    /// <summary>
//    /// 🗑️ Bulk soft-deletes employees by IDs.
//    /// </summary>
//    [HttpPost("bulk-delete")]
//    public async Task<IActionResult> BulkDeleteEmployees([FromBody] long[] ids)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        var employees = await _context.Employees
//            .Where(e => ids.Contains(e.Id))
//            .ToListAsync();

//        var errors = new List<BulkOperationErrorDTO>();
//        var deletedCount = 0;

//        foreach (var employee in employees)
//        {
//            if (!isGlobalAdmin && employee.CompanyId != _currentUser.CompanyId)
//            {
//                errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = -1,
//                    Field = "CompanyId",
//                    Code = "Unauthorized",
//                    Message = $"Zugriff auf Mitarbeiter {employee.Id} nicht erlaubt."
//                });
//                continue;
//            }

//            if (employee.IsDeleted)
//                continue;

//            employee.IsDeleted = true;
//            employee.UpdatedAt = DateTime.UtcNow;
//            employee.UpdatedBy = _currentUser.UserId;
//            deletedCount++;
//        }

//        await _context.SaveChangesAsync();

//        _logger.LogWarning("🗑️ Bulk-soft-deleted {Count} employees by user {UserId}", deletedCount, _currentUser.UserId);

//        return Ok(new BulkOperationResultDTO<EmployeeDTO>
//        {
//            TotalRows = ids.Length,
//            ImportedCount = deletedCount,
//            Errors = errors
//        });
//    }

//    #endregion

//    #region 📥 Import / 📤 Export

//    /// <summary>
//    /// 📤 Exports all employees as a CSV file.
//    /// </summary>
//    [HttpGet("export")]
//    [Produces("text/csv")]
//    public async Task<IActionResult> ExportEmployees()
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");

//        var query = _context.Employees.AsNoTracking();

//        /// 🔐 Einschränkung auf eigene Firma
//        if (!isGlobalAdmin)
//        {
//            if (_currentUser.CompanyId == null)
//                return Forbid("❌ No company restriction.");
//            query = query.Where(e => e.CompanyId == _currentUser.CompanyId);
//        }

//        var employees = await query
//            .OrderBy(e => e.Name)
//            .ToListAsync();

//        /// 🧾 CSV-Datei zusammenbauen
//        var csv = new StringBuilder();
//        csv.AppendLine("Name,Email,Role,CompanyId,UserId");

//        foreach (var e in employees)
//        {
//            csv.AppendLine($"{e.Name},{e.Email},{e.Role},{e.CompanyId},{e.UserId}");
//        }

//        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "employees.csv");
//    }

//    /// <summary>
//    /// 📥 Imports employees from a CSV file.
//    /// </summary>
//    [HttpPost("import")]
//    [RequestSizeLimit(10 * 1024 * 1024)] // 📦 Max. 10MB
//    public async Task<IActionResult> ImportEmployees(IFormFile file)
//    {
//        var isGlobalAdmin = _currentUser.IsInRole("GlobalAdmin");
//        var result = new BulkOperationResultDTO<EmployeeDTO>();
//        var rowCounter = 0;
//        var validList = new List<Employee>();

//        using var reader = new StreamReader(file.OpenReadStream()); // 📂 Datei lesen
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture); // 📊 CSV initialisieren
//        csv.Context.RegisterClassMap<EmployeeImportMap>(); // 🔧 Import-Mapping anwenden

//        while (await csv.ReadAsync())
//        {
//            var record = csv.GetRecord<EmployeeImportDTO>();
//            rowCounter++;

//            try
//            {
//                /// ❌ Pflichtfeld Email
//                if (string.IsNullOrWhiteSpace(record.Email))
//                {
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = "E-Mail ist erforderlich.",
//                        Field = nameof(record.Email),
//                        Code = "REQUIRED_FIELD"
//                    });
//                    continue;
//                }

//                /// ❌ Dublette prüfen
//                var exists = await _context.Employees.AnyAsync(e => e.Email == record.Email);
//                if (exists)
//                {
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = $"E-Mail {record.Email} ist bereits vorhanden.",
//                        Field = nameof(record.Email),
//                        Code = "DUPLICATE"
//                    });
//                    continue;
//                }

//                /// ✅ Employee erstellen
//                validList.Add(new Employee
//                {
//                    Name = record.Name.Trim(),
//                    Email = record.Email.Trim(),
//                    Role = record.Role,
//                    UserId = record.UserId,
//                    CompanyId = isGlobalAdmin ? record.CompanyId : _currentUser.CompanyId,
//                    CreatedAt = DateTime.UtcNow,
//                    CreatedBy = _currentUser.UserId
//                });

//                result.ImportedCount++;
//            }
//            catch (Exception ex)
//            {
//                result.Errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = rowCounter,
//                    Message = $"CSV-Fehler: {ex.Message}",
//                    Field = nameof(record.Email),
//                    Code = "CSV_EXCEPTION"
//                });
//            }
//        }

//        /// 💾 Speichern, wenn gültige Einträge vorhanden sind
//        if (validList.Any())
//        {
//            _context.Employees.AddRange(validList);
//            await _context.SaveChangesAsync();
//        }

//        result.TotalRows = rowCounter;
//        return Ok(result);
//    }

//    /// <summary>
//    /// 🧪 Validates a single CSV record for import.
//    /// </summary>
//    private ValidationResultDTO ValidateImportRecord(EmployeeImportDTO record, int row)
//    {
//        var result = new ValidationResultDTO { IsValid = true };

//        /// ❌ Email prüfen
//        if (string.IsNullOrWhiteSpace(record.Email))
//        {
//            result.Errors.Add("Email is required");
//            result.IsValid = false;
//        }
//        else if (!new EmailAddressAttribute().IsValid(record.Email))
//        {
//            result.Errors.Add("Invalid email format");
//            result.IsValid = false;
//        }

//        /// ❌ Name prüfen
//        if (string.IsNullOrWhiteSpace(record.Name))
//        {
//            result.Errors.Add("Name is required");
//            result.IsValid = false;
//        }

//        /// ❌ UserId prüfen
//        if (record.UserId <= 0)
//        {
//            result.Errors.Add("Invalid User ID");
//            result.IsValid = false;
//        }

//        /// 🧾 Fehlermeldung zusammenbauen
//        if (!result.IsValid)
//        {
//            result.ErrorMessage = $"Row {row} errors: " + string.Join(", ", result.Errors);
//        }

//        return result;
//    }

//    #endregion
//}

