/// @file UserController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-05
/// @brief API controller for admin-managed user accounts
/// @details Provides endpoints to manage users (CRUD, import/export, bulk ops, restore) for Admins and GlobalAdmins only.
/// @endpoints
/// - GET    /api/users               → Get all users (optionally with company info via ?includeCompany=true)
/// - GET    /api/users/{id}          → Get single user by ID (with company info)
/// - PUT    /api/users               → Update user (send full UpdateUserDTO)
/// - DELETE /api/users/{id}          → Soft delete user
/// - POST   /api/users/{id}/restore  → Restore soft-deleted user
/// - POST   /api/users/admin/create  → Admin creates new user (with role assignment)
/// - POST   /api/users/bulk          → Bulk create users (JSON array)
/// - POST   /api/users/import        → Import users from CSV (form-data with file)
/// - GET    /api/users/export        → Export users to CSV (with ?includeCompany=true)


using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using UserFlow.API.Data;
using UserFlow.API.Data.Entities;
using UserFlow.API.Services.Interfaces;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Controllers;

#region 🔐 Authorization & Routing

/// 🔐 Only Admins and GlobalAdmins can access any endpoint here
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin,GlobalAdmin")]
public class UserController : ControllerBase

#endregion
{
    #region 🔒 Fields

    /// 👥 UserManager for identity operations (create, update, roles)
    private readonly UserManager<User> _userManager;

    /// 💾 EF Core database context
    private readonly AppDbContext _context;

    /// 🪵 Logger instance for logging actions
    private readonly ILogger<UserController> _logger;

    /// 👤 Service to access current authenticated user info
    private readonly ICurrentUserService _currentUser;

    #endregion

    #region 🔗 Allowed Includes

    /// 🧩 Allowed include for GET endpoints (currently: Company)
    private const string _allowedIncludes = "Company";

    #endregion

    #region 🔧 Constructor

    /// <summary>
    /// 🛠 Constructor to inject services
    /// </summary>
    public UserController(AppDbContext context, UserManager<User> userManager, ICurrentUserService currentUser, ILogger<UserController> logger)
    {
        _context = context;               // 💾 Assign EF context
        _currentUser = currentUser;       // 👤 Assign current user service
        _logger = logger;                 // 🪵 Assign logger
        _userManager = userManager;       // 👥 Assign identity manager
    }

    #endregion

    #region 📄 CRUD – Read, Update, Delete, Restore

    /// <summary>
    /// 📄 Get a list of all users
    /// </summary>
    /// <param name="includeCompany">true to include company info</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllAsync([FromQuery] bool includeCompany = false)
    {
        /// 🔍 Load full user object from database (read-only)
        var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == _currentUser.UserId);

        if (currentUser == null)
        {
            _logger.LogWarning("❌ Current user (ID={UserId}) not found", _currentUser.UserId);
            return Unauthorized(); // ❌ No valid user context
        }

        var query = _context.Users.AsQueryable().AsNoTracking(); // 🧮 Start query

        if (_currentUser.UserId != 1 && currentUser.CompanyId.HasValue)
            query = query.Where(u => u.CompanyId == currentUser.CompanyId); // 🔐 Limit to same company (except ID 1)

        if (includeCompany)
            query = query.Include(u => u.Company); // 🏢 Optionally include Company

        var users = await query
            .Select(UserMapper.ToUserDto(includeCompany)) // 🧠 Project to DTOs
            .ToListAsync();                               // 🚀 Execute query

        _logger.LogInformation("👥 Retrieved {Count} users for user {UserId} (includeCompany: {Include})",
            users.Count, _currentUser.UserId, includeCompany); // 🪵 Log result

        return Ok(users); // ✅ Return result
    }

    /// <summary>
    /// 📄 Get a single user by ID
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDTO>> GetByIdAsync(long id, [FromQuery] bool includeCompany = false)
    {
        _logger.LogInformation("📄 [GET] /api/users/{Id} requested by UserId={UserId}, includeCompany={IncludeCompany}", id, _currentUser.UserId, includeCompany);

        var query = _context.Users.AsQueryable().AsNoTracking(); // 🧮 Start base query

        if (includeCompany)
            query = query.Include(u => u.Company); // 🏢 Optional include

        var user = await query
            .Where(u => u.Id == id)                              // 🔍 Filter by ID
            .Select(UserMapper.ToUserDto(includeCompany))        // 🧠 Project to DTO
            .FirstOrDefaultAsync();                              // 🚀 Execute

        if (user == null)
        {
            _logger.LogWarning("❌ User ID={Id} not found", id);
            return NotFound(); // ❌ Not found
        }

        _logger.LogInformation("👤 Retrieved user with ID {Id} (includeCompany: {Include})", id, includeCompany);
        return Ok(user); // ✅ Return found user
    }

    /// <summary>
    /// ✏️ Update an existing user
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UserUpdateDTO updateDto)
    {
        _logger.LogInformation("✏️ [PUT] /api/users – Update request for ID={Id} by UserId={UserId}", updateDto.Id, _currentUser.UserId);

        var user = await _userManager.FindByIdAsync(updateDto.Id.ToString()); // 🔍 Load user by ID
        if (user == null)
        {
            _logger.LogWarning("❌ User ID={Id} not found for update", updateDto.Id);
            return NotFound(); // ❌ No such user
        }

        /// ✏️ Update user fields
        user.Name = updateDto.Name.Trim();
        user.Email = updateDto.Email.Trim();
        user.UserName = updateDto.Email.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user); // 💾 Save changes

        if (!result.Succeeded)
        {
            _logger.LogWarning("❌ Failed to update user ID={Id}: {Errors}", updateDto.Id, result.Errors);
            return BadRequest(result.Errors); // ❌ Show identity errors
        }

        _logger.LogInformation("✅ Updated user {Id} ({Email}) by UserId={UserId}", user.Id, user.Email, _currentUser.UserId); // 🪵 Log
        return Ok(new { message = "✅ Benutzer aktualisiert." }); // ✅ Success message
    }

    /// <summary>
    /// 🗑️ Soft delete user by ID
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        _logger.LogInformation("🗑️ [DELETE] /api/users/{Id} requested by UserId={UserId}", id, _currentUser.UserId);

        var user = await _context.Users.FindAsync(id); // 🔍 Load user

        if (user == null)
        {
            _logger.LogWarning("❌ User ID={Id} not found for deletion", id);
            return NotFound(); // ❌ Not found
        }

        user.IsDeleted = true;                         // 🗑️ Mark as deleted
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();             // 💾 Save changes

        _logger.LogWarning("🗑️ User soft deleted: ID={Id} by UserId={UserId}", id, _currentUser.UserId); // 🪵 Log

        return NoContent(); // ✅ No Content
    }

    /// <summary>
    /// ♻️ Restore a soft-deleted user
    /// </summary>
    [HttpPost("{id:long}/restore")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> RestoreAsync(long id)
    {
        _logger.LogInformation("♻️ [POST] /api/users/{Id}/restore requested by UserId={UserId}", id, _currentUser.UserId);

        var user = await _context.Users
            .IgnoreQueryFilters()                      // ⚠️ Include deleted records
            .FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted); // 🔍 Match deleted user

        if (user == null)
        {
            _logger.LogWarning("❌ Cannot restore – user ID={Id} not found or not deleted", id);
            return NotFound(); // ❌ Not found or not deleted
        }

        user.IsDeleted = false;                        // ♻️ Restore
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();             // 💾 Commit changes

        _logger.LogInformation("✅ Restored user ID={Id} by UserId={UserId}", id, _currentUser.UserId); // 🪵 Log
        return Ok(new { message = "✅ User restored successfully." }); // ✅ Return success
    }

    #endregion

    #region 🆕 Admin-Anlage – Create user by admin

    /// <summary>
    /// 🆕 Create a new user via Admin or GlobalAdmin
    /// </summary>
    [HttpPost("admin/create")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> CreateByAdminAsync([FromBody] UserCreateByAdminDTO dto)
    {
        _logger.LogInformation("🆕 Admin user creation requested: Email={Email}, Role={Role}, by UserId={UserId}", dto.Email, dto.Role, _currentUser.UserId);

        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest("❌ Email is required."); // ❌ Validate email

        if (await _userManager.FindByEmailAsync(dto.Email.Trim()) is not null)
            return BadRequest("❌ A user with this email already exists."); // ❌ Check for duplicates

        var allowedRoles = new[] { "User", "Manager", "Admin", "GlobalAdmin" };
        if (!allowedRoles.Contains(dto.Role))
            return BadRequest($"❌ Invalid role: '{dto.Role}'."); // ❌ Validate role

        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser == null)
            return Unauthorized("❌ Current user not found."); // ❌ Invalid session

        var isGlobalAdmin = await _userManager.IsInRoleAsync(currentUser, "GlobalAdmin");
        if (dto.Role == "GlobalAdmin" && !isGlobalAdmin)
            return Forbid("❌ Only GlobalAdmin can assign the 'GlobalAdmin' role."); // 🔐 Security check

        var user = new User
        {
            Email = dto.Email.Trim(),
            UserName = dto.Email.Trim(),
            Name = dto.Name.Trim(),
            NeedsPasswordSetup = true,     // 🔐 User must set password later
            IsActive = true,
            CompanyId = currentUser.CompanyId,
            CreatedAt = DateTime.UtcNow,
            Role = dto.Role
        };

        var result = await _userManager.CreateAsync(user); // 💾 Create user

        if (!result.Succeeded)
        {
            _logger.LogWarning("❌ Failed to create user {Email}: {Errors}", dto.Email, result.Errors);
            return BadRequest(result.Errors); // ❌ Identity errors
        }

        await _userManager.AddToRoleAsync(user, dto.Role); // 🔐 Assign role

        _logger.LogInformation("✅ Created user {Email} with role {Role} by admin {AdminId}", dto.Email, dto.Role, currentUserId);
        return Ok(new { message = "✅ User created successfully." }); // ✅ Success
    }

    #endregion

    #region 📦 Bulk – Create multiple users

    /// <summary>
    /// 📦 Bulk create users from admin
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkCreateAsync([FromBody] List<UserCreateByAdminDTO> list)
    {
        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentAdmin = await _userManager.FindByIdAsync(currentUserId.ToString());

        _logger.LogInformation("📦 Bulk create requested by AdminId={AdminId} with {Count} entries", currentUserId, list.Count);

        var result = new BulkOperationResultDTO<UserDTO>();
        var valid = new List<User>();
        var errors = new List<BulkOperationErrorDTO>();

        foreach (var (dto, index) in list.Select((dto, i) => (dto, i + 1))) // ➕ Enumerate with index
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

            if (await _userManager.FindByEmailAsync(dto.Email.Trim()) is not null)
            {
                errors.Add(new BulkOperationErrorDTO
                {
                    RecordIndex = index,
                    Field = nameof(dto.Email),
                    Code = "DuplicateEmail",
                    Message = $"E-Mail {dto.Email} ist bereits registriert."
                });
                continue;
            }

            valid.Add(new User
            {
                Email = dto.Email.Trim(),
                UserName = dto.Email.Trim(),
                Name = dto.Name.Trim(),
                NeedsPasswordSetup = true,
                IsActive = true,
                CompanyId = currentAdmin?.CompanyId,
                CreatedAt = DateTime.UtcNow,
                Role = dto.Role
            });
        }

        foreach (var user in valid)
        {
            await _userManager.CreateAsync(user);              // 💾 Create
            await _userManager.AddToRoleAsync(user, user.Role); // 🔐 Role
        }

        result.TotalRows = list.Count;
        result.ImportedCount = valid.Count;
        result.Errors = errors;

        _logger.LogInformation("✅ Bulk created {Count} users by AdminId={AdminId}", valid.Count, currentUserId);
        return Accepted(result); // ✅ Return 202 Accepted with result
    }

    #endregion

    #region 📥 Import / 📤 Export Users via CSV

    /// <summary>
    /// 📥 Imports users from a CSV file (Admin only)
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportAsync(IFormFile file)
    {
        _logger.LogInformation("📥 Import requested by AdminId={AdminId}", _currentUser.UserId);

        var result = new BulkOperationResultDTO<UserDTO>();
        var validList = new List<User>();
        var rowCounter = 0;

        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminUser = await _userManager.FindByIdAsync(currentUserId.ToString());

        using var reader = new StreamReader(file.OpenReadStream());           // 📂 Open CSV stream
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);  // 🌍 Use invariant culture
        csv.Context.RegisterClassMap<UserImportMap>();                        // 🗺 CSV mapping

        while (await csv.ReadAsync()) // 🔄 Read rows
        {
            rowCounter++;
            try
            {
                var record = csv.GetRecord<UserImportDTO>(); // 🧾 Parse row

                if (string.IsNullOrWhiteSpace(record.Email))
                {
                    result.Errors.Add(new BulkOperationErrorDTO
                    {
                        RecordIndex = rowCounter,
                        Message = "E-Mail ist erforderlich",
                        Field = nameof(record.Email),
                        Code = "REQUIRED_FIELD"
                    });
                    continue;
                }

                if (await _userManager.FindByEmailAsync(record.Email.Trim()) is not null)
                {
                    result.Errors.Add(new BulkOperationErrorDTO
                    {
                        RecordIndex = rowCounter,
                        Message = $"E-Mail {record.Email} ist bereits vorhanden",
                        Field = nameof(record.Email),
                        Code = "DUPLICATE"
                    });
                    continue;
                }

                validList.Add(new User
                {
                    Email = record.Email.Trim(),
                    UserName = record.Email.Trim(),
                    Name = record.Name.Trim(),
                    NeedsPasswordSetup = true,
                    IsActive = true,
                    CompanyId = adminUser?.CompanyId,
                    CreatedAt = DateTime.UtcNow,
                    Role = "User" // 👤 Default role
                });

                result.ImportedCount++; // ➕ Count success
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CSV parse error at line {Line}", rowCounter);
                result.Errors.Add(new BulkOperationErrorDTO
                {
                    RecordIndex = rowCounter,
                    Message = $"CSV-Fehler: {ex.Message}",
                    Code = "CSV_PARSE_ERROR"
                });
            }
        }

        foreach (var user in validList)
        {
            await _userManager.CreateAsync(user);
            await _userManager.AddToRoleAsync(user, user.Role);
        }

        result.TotalRows = rowCounter;
        _logger.LogInformation("✅ Imported {Count} users from CSV by AdminId={AdminId}", result.ImportedCount, currentUserId);
        return Ok(result); // ✅ Return full import result
    }

    /// <summary>
    /// 📤 Exports all users to CSV
    /// </summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportAsync([FromQuery] bool includeCompany = false)
    {
        _logger.LogInformation("📤 Export requested by AdminId={AdminId} (includeCompany={Include})", _currentUser.UserId, includeCompany);

        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .Select(UserMapper.ToUserDto(includeCompany))
            .OrderBy(u => u.Name)
            .ToListAsync(); // 🚀 Load all users

        var csv = new StringBuilder(); // 🧱 Build CSV content
        csv.AppendLine("Name,Email,Role,NeedsPasswordSetup,IsActive,CompanyName");

        foreach (var user in users)
        {
            var line = $"{user.Name},{user.Email},{user.Role},{user.NeedsPasswordSetup},{user.IsActive},{user.CompanyName}";
            csv.AppendLine(line); // 🧾 Add CSV line
        }

        _logger.LogInformation("✅ Exported {Count} users to CSV", users.Count);
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "users.csv"); // 📤 Return file
    }

    #endregion
}


///// @file UserController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-05
///// @brief API controller for admin-managed user accounts
///// @details Provides endpoints to manage users (CRUD, import/export, bulk ops, restore) for Admins and GlobalAdmins only.
///// @endpoints
///// - GET    /api/users               → Get all users (optionally with company info)
///// - GET    /api/users/{id}          → Get single user by ID
///// - PUT    /api/users               → Update user
///// - DELETE /api/users/{id}          → Soft delete user
///// - POST   /api/users/{id}/restore  → Restore soft-deleted user
///// - POST   /api/users/admin/create  → Admin creates a new user
///// - POST   /api/users/bulk          → Bulk create users
///// - POST   /api/users/import        → Import users from CSV
///// - GET    /api/users/export        → Export users to CSV

//using CsvHelper;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.CodeAnalysis;
//using Microsoft.EntityFrameworkCore;
//using System.Globalization;
//using System.Security.Claims;
//using System.Text;
//using UserFlow.API.Data;
//using UserFlow.API.Data.Entities;
//using UserFlow.API.Services.Interfaces;
//using UserFlow.API.Shared.DTO;

//namespace UserFlow.API.Controllers;

//#region 🔐 Authorization & Routing

///// 🔐 Only Admins and GlobalAdmins can access any endpoint here
//[ApiController]
//[Route("api/users")]
//[Authorize(Roles = "Admin,GlobalAdmin")]
//public class UserController : ControllerBase

//#endregion
//{
//    #region 🔒 Fields

//    /// 👥 UserManager for identity operations (create, update, roles)
//    private readonly UserManager<User> _userManager;

//    /// 💾 EF Core database context
//    private readonly AppDbContext _context;

//    /// 🪵 Logger instance for logging actions
//    private readonly ILogger<UserController> _logger;

//    /// 👤 Service to access current authenticated user info
//    private readonly ICurrentUserService _currentUser;

//    #endregion

//    #region 🔗 Allowed Includes

//    /// 🧩 Allowed include for GET endpoints (currently: Company)
//    private const string _allowedIncludes = "Company";

//    #endregion

//    #region 🔧 Constructor

//    /// 🛠 Constructor to inject services
//    public UserController(AppDbContext context, UserManager<User> userManager, ICurrentUserService currentUser, ILogger<UserController> logger)
//    {
//        _context = context;               // 💾 Assign EF context
//        _currentUser = currentUser;       // 👤 Assign current user service
//        _logger = logger;                 // 🪵 Assign logger
//        _userManager = userManager;       // 👥 Assign identity manager
//    }

//    #endregion

//    #region 📄 CRUD – Read, Update, Delete, Restore

//    /// 📄 Get a list of all users
//    /// @param includeCompany true to include company info
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers([FromQuery] bool includeCompany = false)
//    {
//        /// 🔍 Extract current user's ID from claims
//        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//        /// 🔍 Load full user object from database (read-only)
//        var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);

//        if (currentUser == null)
//            return Unauthorized(); // ❌ No valid user context

//        var query = _context.Users.AsQueryable().AsNoTracking(); // 🧮 Start query

//        if (currentUserId != 1 && currentUser.CompanyId.HasValue)
//            query = query.Where(u => u.CompanyId == currentUser.CompanyId); // 🔐 Limit to same company (except ID 1)

//        if (includeCompany)
//            query = query.Include(u => u.Company); // 🏢 Optionally include Company

//        var users = await query
//            .Select(UserMapper.ToUserDto(includeCompany)) // 🧠 Project to DTOs
//            .ToListAsync();                               // 🚀 Execute query

//        _logger.LogInformation("👥 Retrieved {Count} users for user {UserId} (includeCompany: {Include})",
//            users.Count, currentUserId, includeCompany); // 🪵 Log result

//        return Ok(users); // ✅ Return result
//    }

//    /// 📄 Get a single user by ID
//    [HttpGet("{id:long}")]
//    public async Task<ActionResult<UserDTO>> GetUser(long id, [FromQuery] bool includeCompany = false)
//    {
//        var query = _context.Users.AsQueryable().AsNoTracking(); // 🧮 Start base query

//        if (includeCompany)
//            query = query.Include(u => u.Company); // 🏢 Optional include

//        var user = await query
//            .Where(u => u.Id == id)                              // 🔍 Filter by ID
//            .Select(UserMapper.ToUserDto(includeCompany))        // 🧠 Project to DTO
//            .FirstOrDefaultAsync();                              // 🚀 Execute

//        if (user == null)
//            return NotFound(); // ❌ Not found

//        _logger.LogInformation("👤 Retrieved user with ID {Id} (includeCompany: {Include})", id, includeCompany);
//        return Ok(user); // ✅ Return found user
//    }

//    /// ✏️ Update an existing user
//    [HttpPut]
//    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDTO updateDto)
//    {
//        var user = await _userManager.FindByIdAsync(updateDto.Id.ToString()); // 🔍 Load user by ID
//        if (user == null)
//            return NotFound(); // ❌ No such user

//        /// ✏️ Update user fields
//        user.Name = updateDto.Name.Trim();
//        user.Email = updateDto.Email.Trim();
//        user.UserName = updateDto.Email.Trim();
//        user.UpdatedAt = DateTime.UtcNow;

//        var result = await _userManager.UpdateAsync(user); // 💾 Save changes

//        if (!result.Succeeded)
//            return BadRequest(result.Errors); // ❌ Show identity errors

//        _logger.LogInformation("✏️ Updated user {Id} ({Email})", user.Id, user.Email); // 🪵 Log
//        return Ok(new { message = "✅ Benutzer aktualisiert." }); // ✅ Success message
//    }

//    /// 🗑️ Soft delete user by ID
//    [HttpDelete("{id:long}")]
//    public async Task<IActionResult> DeleteUser(long id)
//    {
//        var user = await _context.Users.FindAsync(id); // 🔍 Load user

//        if (user == null)
//            return NotFound(); // ❌ Not found

//        user.IsDeleted = true;                         // 🗑️ Mark as deleted
//        user.UpdatedAt = DateTime.UtcNow;

//        await _context.SaveChangesAsync();             // 💾 Save changes

//        _logger.LogWarning("🗑️ User soft deleted: ID={Id} by UserId={UserId}", id, _currentUser.UserId); // 🪵 Log

//        return NoContent(); // ✅ No Content
//    }

//    /// ♻️ Restore a soft-deleted user
//    [HttpPost("{id:long}/restore")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> RestoreUser(long id)
//    {
//        var user = await _context.Users
//            .IgnoreQueryFilters()                      // ⚠️ Include deleted records
//            .FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted); // 🔍 Match deleted user

//        if (user == null)
//            return NotFound(); // ❌ Not found or not deleted

//        user.IsDeleted = false;                        // ♻️ Restore
//        user.UpdatedAt = DateTime.UtcNow;

//        await _context.SaveChangesAsync();             // 💾 Commit changes

//        _logger.LogInformation("♻️ Restored user ID={Id} by UserId={UserId}", id, _currentUser.UserId); // 🪵 Log
//        return Ok(new { message = "✅ User restored successfully." }); // ✅ Return success
//    }

//    #endregion

//    #region 🆕 Admin-Anlage – Create user by admin

//    /// 🆕 Create a new user via Admin or GlobalAdmin
//    [HttpPost("admin/create")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> CreateUserByAdmin([FromBody] CreateUserByAdminDTO dto)
//    {
//        if (string.IsNullOrWhiteSpace(dto.Email))
//            return BadRequest("❌ Email is required."); // ❌ Validate email

//        if (await _userManager.FindByEmailAsync(dto.Email.Trim()) is not null)
//            return BadRequest("❌ A user with this email already exists."); // ❌ Check for duplicates

//        var allowedRoles = new[] { "User", "Manager", "Admin", "GlobalAdmin" };
//        if (!allowedRoles.Contains(dto.Role))
//            return BadRequest($"❌ Invalid role: '{dto.Role}'."); // ❌ Validate role

//        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());

//        if (currentUser == null)
//            return Unauthorized("❌ Current user not found."); // ❌ Invalid session

//        var isGlobalAdmin = await _userManager.IsInRoleAsync(currentUser, "GlobalAdmin");
//        if (dto.Role == "GlobalAdmin" && !isGlobalAdmin)
//            return Forbid("❌ Only GlobalAdmin can assign the 'GlobalAdmin' role."); // 🔐 Security check

//        var user = new User
//        {
//            Email = dto.Email.Trim(),
//            UserName = dto.Email.Trim(),
//            Name = dto.Name.Trim(),
//            NeedsPasswordSetup = true,     // 🔐 User must set password later
//            IsActive = true,
//            CompanyId = currentUser.CompanyId,
//            CreatedAt = DateTime.UtcNow,
//            Role = dto.Role
//        };

//        var result = await _userManager.CreateAsync(user); // 💾 Create user

//        if (!result.Succeeded)
//            return BadRequest(result.Errors); // ❌ Identity errors

//        await _userManager.AddToRoleAsync(user, dto.Role); // 🔐 Assign role

//        _logger.LogInformation("🆕 Created user {Email} with role {Role} by admin {AdminId}", dto.Email, dto.Role, currentUserId);
//        return Ok(new { message = "✅ User created successfully." }); // ✅ Success
//    }

//    #endregion

//    #region 📦 Bulk – Create multiple users

//    /// 📦 Bulk create users from admin
//    [HttpPost("bulk")]
//    [Authorize(Roles = "Admin")]
//    public async Task<IActionResult> BulkCreateUsers([FromBody] List<CreateUserByAdminDTO> list)
//    {
//        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var currentAdmin = await _userManager.FindByIdAsync(currentUserId.ToString());

//        var result = new BulkOperationResultDTO<UserDTO>();
//        var valid = new List<User>();
//        var errors = new List<BulkOperationErrorDTO>();

//        foreach (var (dto, index) in list.Select((dto, i) => (dto, i + 1))) // ➕ Enumerate with index
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

//            if (await _userManager.FindByEmailAsync(dto.Email.Trim()) is not null)
//            {
//                errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = index,
//                    Field = nameof(dto.Email),
//                    Code = "DuplicateEmail",
//                    Message = $"E-Mail {dto.Email} ist bereits registriert."
//                });
//                continue;
//            }

//            valid.Add(new User
//            {
//                Email = dto.Email.Trim(),
//                UserName = dto.Email.Trim(),
//                Name = dto.Name.Trim(),
//                NeedsPasswordSetup = true,
//                IsActive = true,
//                CompanyId = currentAdmin?.CompanyId,
//                CreatedAt = DateTime.UtcNow,
//                Role = dto.Role
//            });
//        }

//        foreach (var user in valid)
//        {
//            await _userManager.CreateAsync(user);              // 💾 Create
//            await _userManager.AddToRoleAsync(user, user.Role); // 🔐 Role
//        }

//        result.TotalRows = list.Count;
//        result.ImportedCount = valid.Count;
//        result.Errors = errors;

//        _logger.LogInformation("📦 Bulk created {Count} users by Admin {AdminId}", valid.Count, currentUserId);
//        return Accepted(result); // ✅ Return 202 Accepted with result
//    }

//    #endregion

//    #region 📥 Import / 📤 Export Users via CSV

//    /// 📥 Import users from CSV file (Admins only)
//    [HttpPost("import")]
//    [Authorize(Roles = "Admin")]
//    [RequestSizeLimit(10 * 1024 * 1024)]
//    public async Task<IActionResult> ImportUsers(IFormFile file)
//    {
//        var result = new BulkOperationResultDTO<UserDTO>();
//        var validList = new List<User>();
//        var rowCounter = 0;

//        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var adminUser = await _userManager.FindByIdAsync(currentUserId.ToString());

//        using var reader = new StreamReader(file.OpenReadStream());           // 📂 Open CSV stream
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);  // 🌍 Use invariant culture
//        csv.Context.RegisterClassMap<UserImportMap>();                        // 🗺 CSV mapping

//        while (await csv.ReadAsync()) // 🔄 Read rows
//        {
//            rowCounter++;
//            try
//            {
//                var record = csv.GetRecord<UserImportDTO>(); // 🧾 Parse row

//                if (string.IsNullOrWhiteSpace(record.Email))
//                {
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = "E-Mail ist erforderlich",
//                        Field = nameof(record.Email),
//                        Code = "REQUIRED_FIELD"
//                    });
//                    continue;
//                }

//                if (await _userManager.FindByEmailAsync(record.Email.Trim()) is not null)
//                {
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = $"E-Mail {record.Email} ist bereits vorhanden",
//                        Field = nameof(record.Email),
//                        Code = "DUPLICATE"
//                    });
//                    continue;
//                }

//                validList.Add(new User
//                {
//                    Email = record.Email.Trim(),
//                    UserName = record.Email.Trim(),
//                    Name = record.Name.Trim(),
//                    NeedsPasswordSetup = true,
//                    IsActive = true,
//                    CompanyId = adminUser?.CompanyId,
//                    CreatedAt = DateTime.UtcNow,
//                    Role = "User" // 👤 Default role
//                });

//                result.ImportedCount++; // ➕ Count success
//            }
//            catch (Exception ex)
//            {
//                result.Errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = rowCounter,
//                    Message = $"CSV-Fehler: {ex.Message}",
//                    Code = "CSV_PARSE_ERROR"
//                });
//            }
//        }

//        foreach (var user in validList)
//        {
//            await _userManager.CreateAsync(user);
//            await _userManager.AddToRoleAsync(user, user.Role);
//        }

//        result.TotalRows = rowCounter;
//        _logger.LogInformation("📥 Imported {Count} users by Admin {AdminId}", result.ImportedCount, currentUserId);
//        return Ok(result); // ✅ Return full import result
//    }

//    /// 📤 Export all users to CSV
//    [HttpGet("export")]
//    [Produces("text/csv")]
//    public async Task<IActionResult> ExportUsers([FromQuery] bool includeCompany = false)
//    {
//        var users = await _context.Users
//            .AsNoTracking()
//            .Include(u => u.Company)
//            .Select(UserMapper.ToUserDto(includeCompany))
//            .OrderBy(u => u.Name)
//            .ToListAsync(); // 🚀 Load all users

//        var csv = new StringBuilder(); // 🧱 Build CSV content
//        csv.AppendLine("Name,Email,Role,NeedsPasswordSetup,IsActive,CompanyName");

//        foreach (var user in users)
//        {
//            var line = $"{user.Name},{user.Email},{user.Role},{user.NeedsPasswordSetup},{user.IsActive},{user.CompanyName}";
//            csv.AppendLine(line); // 🧾 Add CSV line
//        }

//        _logger.LogInformation("📤 Exported {Count} users", users.Count);
//        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "users.csv"); // 📤 Return file
//    }

//    #endregion
//}
