/// @file DashboardController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-04
/// @brief Admin Dashboard controller providing user/project statistics, import/export and overviews.
/// @details
/// Provides role-based dashboard data for Admins and GlobalAdmins, including user and project counts,
/// latest user records, CSV export of users, and bulk import of user emails. Also includes error-handling,
/// logging, and multi-tenancy logic.
///
/// @endpoints
/// - GET    /api/dashboard/users/count        → Returns the number of users for the current tenant (or all for GlobalAdmin)
/// - GET    /api/dashboard/projects/count     → Returns the number of projects for the current tenant (or all for GlobalAdmin)
/// - GET    /api/dashboard/users/latest       → Returns the N most recently created users
/// - GET    /api/dashboard/export/users       → Export user list as CSV (Name, Email, CompanyId)
/// - POST   /api/dashboard/import/users       → Import user email list from CSV (validates structure only)

using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using UserFlow.API.Data;
using UserFlow.API.Services.Interfaces;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,GlobalAdmin")]
public class DashboardController : ControllerBase
{
    #region 🔒 Private Fields

    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DashboardController> _logger; // Korrektur des generischen Typs

    #endregion

    #region 🔧 Constructor

    public DashboardController(
        AppDbContext context,
        ICurrentUserService currentUser,
        ILogger<DashboardController> logger) // Korrigierter Logger-Typ
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    #endregion

    #region 📊 Statistics

    [HttpGet("users/count")]
    public async Task<ActionResult<int>> GetUserCount()
    {
        try
        {
            var query = _context.Users.AsQueryable();

            if (!_currentUser.IsInRole("GlobalAdmin"))
            {
                if (_currentUser.CompanyId.HasValue)
                {
                    query = query.Where(u => u.CompanyId == _currentUser.CompanyId);
                }
                else
                {
                    _logger.LogWarning("🚫 User count request denied for {UserId} (no company assigned)", _currentUser.UserId);
                    return Ok(0);
                }
            }

            var count = await query.CountAsync();

            _logger.LogInformation("📊 User count: {Count} for {UserRole} {UserId}",
                count,
                _currentUser.IsInRole("GlobalAdmin") ? "GlobalAdmin" : "Admin",
                _currentUser.UserId);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving user count");
            throw;
        }
    }

    [HttpGet("projects/count")]
    public async Task<ActionResult<int>> GetProjectCount()
    {
        try
        {
            var query = _context.Projects.AsQueryable();

            if (!_currentUser.IsInRole("GlobalAdmin"))
            {
                if (_currentUser.CompanyId.HasValue)
                {
                    query = query.Where(p => p.CompanyId == _currentUser.CompanyId);
                }
                else
                {
                    _logger.LogWarning("🚫 Project count request denied for {UserId} (no company)", _currentUser.UserId);
                    return Ok(0);
                }
            }

            var count = await query.CountAsync();

            _logger.LogInformation("📊 Project count: {Count} for {UserRole} {UserId}",
                count,
                _currentUser.IsInRole("GlobalAdmin") ? "GlobalAdmin" : "Admin",
                _currentUser.UserId);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving project count");
            throw;
        }
    }

    #endregion

    #region 🕵️ Latest Users

    [HttpGet("users/latest")]
    public async Task<ActionResult<IEnumerable<UserDTO>>> GetLatestUsers([FromQuery] int count = 5)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            if (!_currentUser.IsInRole("GlobalAdmin"))
            {
                if (_currentUser.CompanyId.HasValue)
                {
                    query = query.Where(u => u.CompanyId == _currentUser.CompanyId);
                }
                else
                {
                    _logger.LogWarning("🚫 Latest users request denied for {UserId} (no company)", _currentUser.UserId);
                    return Ok(new List<UserDTO>());
                }
            }

            var latest = await query
                .Include(u => u.Company)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .Select(UserMapper.ToUserDto())
                .ToListAsync();

            _logger.LogInformation("🕵️ Retrieved {ResultCount} latest users for {UserId}", latest.Count, _currentUser.UserId);

            return Ok(latest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving latest users");
            throw;
        }
    }

    #endregion

    #region 📥 Import / 📤 Export

    [HttpPost("import/users")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<BulkOperationResultDTO<UserDTO>>> ImportUserEmails(IFormFile file)
    {
        var result = new BulkOperationResultDTO<UserDTO>();
        var rowCounter = 0;

        try
        {
            if (!_currentUser.IsInRole("GlobalAdmin"))
            {
                _logger.LogWarning("🚫 Unauthorized import attempt by {UserId}", _currentUser.UserId);
                return Forbid();
            }

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            while (await csv.ReadAsync())
            {
                rowCounter++;
                try
                {
                    var email = csv.GetField<string>("Email");

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        _logger.LogWarning("📥 Import row {Row}: Missing email", rowCounter);
                        result.Errors.Add(new BulkOperationErrorDTO
                        {
                            RecordIndex = rowCounter,
                            Message = "Email is required.",
                            Code = "MISSING_EMAIL"
                        });
                        continue;
                    }

                    if (!new EmailAddressAttribute().IsValid(email))
                    {
                        _logger.LogWarning("📥 Import row {Row}: Invalid email '{Email}'", rowCounter, email);
                        result.Errors.Add(new BulkOperationErrorDTO
                        {
                            RecordIndex = rowCounter,
                            Message = "Email is invalid.",
                            Code = "INVALID_EMAIL"
                        });
                        continue;
                    }

                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ CSV import error in row {Row}", rowCounter);
                    result.Errors.Add(new BulkOperationErrorDTO
                    {
                        RecordIndex = rowCounter,
                        Message = $"Processing error: {ex.Message}",
                        Code = "PROCESSING_ERROR"
                    });
                }
            }

            result.TotalRows = rowCounter;

            _logger.LogInformation("📥 Import completed by {UserId}: {Success}/{Total} with {Errors} errors",
                _currentUser.UserId,
                result.ImportedCount,
                result.TotalRows,
                result.Errors.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Critical failure during user import");
            throw;
        }
    }

    [HttpGet("export/users")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportUsers()
    {
        try
        {
            var query = _context.Users.AsQueryable();

            if (!_currentUser.IsInRole("GlobalAdmin") && _currentUser.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _currentUser.CompanyId);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Name,
                    u.Email,
                    u.CreatedAt,
                    u.CompanyId
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Name,Email,CreatedAt,CompanyId");

            foreach (var user in users)
            {
                csv.AppendLine($"{EscapeCsv(user.Name)},{EscapeCsv(user.Email!)},{user.CreatedAt:O},{user.CompanyId}");
            }

            _logger.LogInformation("📤 Exported {Count} users by {UserId}", users.Count, _currentUser.UserId);

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"users_export_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CSV export failed for {UserId}", _currentUser.UserId);
            throw;
        }
    }

    #endregion

    #region 🛠️ Helpers

    private static string EscapeCsv(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";

    #endregion
}









///// @file DashboardController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-04
///// @brief Admin Dashboard controller providing user/project statistics, import/export and overviews.
///// @details
///// This controller provides dashboard-related endpoints for Admins and GlobalAdmins,
///// including statistics on users/projects, latest user overview, user import and CSV export.
/////
///// @endpoints
///// - GET /api/dashboard/users/count → Returns user count (filtered by company if not GlobalAdmin)
///// - GET /api/dashboard/projects/count → Returns project count (filtered by company if not GlobalAdmin)
///// - GET /api/dashboard/users/latest → Returns the latest users
///// - POST /api/dashboard/import/users → Imports user emails from CSV
///// - GET /api/dashboard/export/users → Exports user list as CSV

//using CsvHelper;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.ComponentModel.DataAnnotations;
//using System.Globalization;
//using System.Text;
//using UserFlow.API.Data;
//using UserFlow.API.Services.Interfaces;
//using UserFlow.API.Shared.DTO;

//namespace UserFlow.API.Controllers;

//[ApiController] // ✅ Enables model binding and validation
//[Route("api/dashboard")] // 📍 Base route for all dashboard endpoints
//[Authorize(Roles = "Admin,GlobalAdmin")] // 🔐 Restrict access to Admins and GlobalAdmins
//public class DashboardController : ControllerBase
//{
//    #region 🔒 Private Fields

//    private readonly AppDbContext _context; // 🧱 EF Core context for database access
//    private readonly ICurrentUserService _currentUser; // 👤 Current user info (role, company, userId)
//    private readonly ILogger<EmployeeController> _logger; // 📋 Logger instance

//    #endregion

//    #region 🔧 Constructor

//    /// <summary>
//    /// Initializes a new instance of the <see cref=\"DashboardController\"/> class.
//    /// </summary>
//    public DashboardController(AppDbContext context, ICurrentUserService currentUser, ILogger<EmployeeController> logger)
//    {
//        _context = context;         // 💉 Injected database context
//        _currentUser = currentUser; // 💉 Injected current user context
//        _logger = logger;           // 💉 Injected logger
//    }

//    #endregion

//    #region 📊 Statistics

//    /// <summary>
//    /// 📊 Returns the number of users (company-filtered if not GlobalAdmin).
//    /// </summary>
//    [HttpGet("users/count")]
//    public async Task<ActionResult<int>> GetUserCount()
//    {
//        /// 🔍 Start base query
//        var query = _context.Users.AsQueryable();

//        /// 🔐 Filter by company if not GlobalAdmin
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//        {
//            if (_currentUser.CompanyId.HasValue)
//                query = query.Where(u => u.CompanyId == _currentUser.CompanyId);
//            else
//                return Ok(0); // ❌ No company assigned → 0 users
//        }

//        /// 🔢 Count results
//        var count = await query.CountAsync();

//        /// 📝 Log request
//        _logger.LogInformation("📊 User count requested by {UserId}, result: {Count}", _currentUser.UserId, count);

//        return Ok(count);
//    }

//    /// <summary>
//    /// 📊 Returns the number of projects (company-filtered if not GlobalAdmin).
//    /// </summary>
//    [HttpGet("projects/count")]
//    public async Task<ActionResult<int>> GetProjectCount()
//    {
//        /// 🔍 Start query
//        var query = _context.Projects.AsQueryable();

//        /// 🔐 Filter by company if not GlobalAdmin
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//        {
//            if (_currentUser.CompanyId.HasValue)
//                query = query.Where(p => p.CompanyId == _currentUser.CompanyId);
//            else
//                return Ok(0); // ❌ No company assigned
//        }

//        /// 🔢 Count entries
//        var count = await query.CountAsync();

//        /// 📝 Log result
//        _logger.LogInformation("📊 Project count requested by {UserId}, result: {Count}", _currentUser.UserId, count);
//        return Ok(count);
//    }

//    #endregion

//    #region 🕵️ Latest Users

//    /// <summary>
//    /// 🕵️ Returns the latest registered users (limited, optionally company-filtered).
//    /// </summary>
//    /// <summary>
//    /// 🕵️ Returns the latest registered users (limited, optionally company-filtered).
//    /// Includes company name in the result.
//    /// </summary>
//    /// <param name="count">Maximum number of users to return.</param>
//    /// <returns>List of the latest <see cref="UserDTO"/> records.</returns>
//    [HttpGet("users/latest")]
//    public async Task<ActionResult<IEnumerable<UserDTO>>> GetLatestUsers([FromQuery] int count = 5)
//    {
//        /// 🔍 Start base query
//        var query = _context.Users.AsQueryable();

//        /// 🔐 Filter by company if not GlobalAdmin
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//        {
//            if (_currentUser.CompanyId.HasValue)
//                query = query.Where(u => u.CompanyId == _currentUser.CompanyId);
//            else
//                return Ok(new List<UserDTO>()); // ❌ No company assigned
//        }

//        /// 🔄 Get latest N users by CreatedAt with company info
//        var latest = await query
//            .Include(u => u.Company) // 🏢 Required for CompanyName in DTO
//            .OrderByDescending(u => u.CreatedAt)
//            .Take(count)
//            .Select(UserMapper.ToUserDto())
//            .ToListAsync();

//        /// 📝 Log query
//        _logger.LogInformation("🕵️ Latest users requested by {UserId}, count: {Count}", _currentUser.UserId, latest.Count);
//        return Ok(latest);
//    }
//    /*
//    [HttpGet("users/latest")]
//    public async Task<ActionResult<IEnumerable<UserDTO>>> GetLatestUsers([FromQuery] int count = 5)
//    {
//        /// 🔍 Start base query
//        var query = _context.Users.AsQueryable();

//        /// 🔐 Filter by company if not GlobalAdmin
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//        {
//            if (_currentUser.CompanyId.HasValue)
//                query = query.Where(u => u.CompanyId == _currentUser.CompanyId);
//            else
//                return Ok(new List<object>()); // ❌ No company assigned
//        }

//        /// 🔄 Get latest N users by CreatedAt
//        var latest = await query
//            .OrderByDescending(u => u.CreatedAt)
//            .Take(count)
//            .Select(u => new
//            {
//                u.Id,
//                u.Email,
//                u.Name,
//                u.CreatedAt,
//                u.CompanyId
//            })
//            .ToListAsync();

//        /// 📝 Log query
//        _logger.LogInformation("🕵️ Latest users requested by {UserId}, count: {Count}", _currentUser.UserId, latest.Count);
//        return Ok(latest);
//    }
//    */
//    #endregion

//    #region 📥 Import / 📤 Export

//    /// <summary>
//    /// 📥 Imports a list of user emails from a CSV file (validation only).
//    /// </summary>
//    [HttpPost("import/users")]
//    [RequestSizeLimit(10 * 1024 * 1024)] // 📦 Allow files up to 10MB
//    public async Task<ActionResult<BulkOperationResultDTO<UserDTO>>> ImportUserEmails(IFormFile file)
//    {
//        /// 🧾 Prepare result object for import summary
//        var result = new BulkOperationResultDTO<UserDTO>();
//        var rowCounter = 0;

//        /// 🔐 Only GlobalAdmin allowed
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid();

//        /// 📖 Open file for reading
//        using var reader = new StreamReader(file.OpenReadStream());
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

//        /// 🔁 Read line by line
//        while (await csv.ReadAsync())
//        {
//            rowCounter++; // ➕ Line counter

//            try
//            {
//                /// 📥 Get email field from current row
//                var email = csv.GetField<string>("Email");

//                /// ❌ Email is required
//                if (string.IsNullOrWhiteSpace(email))
//                {
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = "Email is required.",
//                        Code = "CSV_PARSE_ERROR"
//                    });
//                    continue;
//                }

//                /// ❌ Validate email format
//                if (!new EmailAddressAttribute().IsValid(email))
//                {
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = "Email is invalid."
//                    });
//                    continue;
//                }

//                /// ✅ Count valid row (for demo or log purposes only)
//                result.ImportedCount++;
//            }
//            catch (Exception ex)
//            {
//                /// 🧨 Catch and record error
//                result.Errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = rowCounter,
//                    Message = $"Import error: {ex.Message}"
//                });
//            }
//        }

//        /// 🔢 Store total number of rows
//        result.TotalRows = rowCounter;

//        /// 📝 Log import summary
//        _logger.LogInformation("📥 User email import by {UserId}: {Success} successful, {Errors} errors",
//            _currentUser.UserId, result.ImportedCount, result.Errors.Count);

//        /// 📤 Return result
//        return Ok(result);
//    }

//    /// <summary>
//    /// 📤 Exports a list of users as CSV.
//    /// </summary>
//    [HttpGet("export/users")]
//    [Produces("text/csv")]
//    public async Task<IActionResult> ExportUsers()
//    {
//        /// 🔍 Start base query
//        var query = _context.Users.AsQueryable();

//        /// 🔐 Filter to current company if not GlobalAdmin
//        if (!_currentUser.IsInRole("GlobalAdmin") && _currentUser.CompanyId.HasValue)
//            query = query.Where(u => u.CompanyId == _currentUser.CompanyId);

//        /// 🔄 Load and shape data
//        var users = await query
//            .OrderByDescending(u => u.CreatedAt)
//            .Select(u => new
//            {
//                u.Name,
//                u.Email,
//                u.CreatedAt,
//                u.CompanyId
//            })
//            .ToListAsync();

//        /// 🧾 Build CSV string
//        var csv = new StringBuilder();
//        csv.AppendLine("Name,Email,CreatedAt,CompanyId");

//        /// ➕ Append user rows
//        foreach (var user in users)
//        {
//            csv.AppendLine($"{user.Name},{user.Email},{user.CreatedAt:O},{user.CompanyId}");
//        }

//        /// 📝 Log export
//        _logger.LogInformation("📤 User export by {UserId}, {Count} rows", _currentUser.UserId, users.Count);

//        /// 📎 Return CSV file
//        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "user_export.csv");
//    }

//    #endregion
//}
