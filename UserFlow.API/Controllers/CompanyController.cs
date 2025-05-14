/// *****************************************************************************************
/// @file CompanyController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Manages company CRUD operations, user access, and CSV import/export.
/// @details Administrative endpoints for managing companies including CRUD,
/// restore, user listing, CSV import/export. GlobalAdmin required.
/// @endpoints
/// - GET    /api/companies                    → Get paged list of companies (optional: includeUsers=true)
/// - GET    /api/companies/{id}               → Get single company by ID (optional: includeUsers=true)
/// - POST   /api/companies                    → Create a new company
/// - PUT    /api/companies                    → Update an existing company
/// - DELETE /api/companies/{id}               → Soft delete a company
/// - PUT    /api/companies/{id}/restore       → Restore a soft-deleted company
/// - GET    /api/companies/{companyId}/users  → List users in company
/// - GET    /api/companies/export             → Export all companies as CSV
/// - POST   /api/companies/import             → Import companies from CSV
/// *****************************************************************************************

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
[Route("api/companies")]
[Authorize(Roles = "GlobalAdmin")]
public class CompanyController : ControllerBase
{
    #region 🔒 Fields

    private readonly AppDbContext _context;
    private readonly ILogger<CompanyController> _logger;
    private readonly ICurrentUserService _currentUser;

    #endregion

    #region 🔧 Constructor

    public CompanyController(AppDbContext context, ICurrentUserService currentUser, ILogger<CompanyController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    #endregion

    #region 📄 CRUD Operations

    [HttpGet]
    public async Task<ActionResult<PagedResultDTO<CompanyDTO>>> GetAllAsync(
        [FromQuery] bool includeUsers = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Companies.AsNoTracking();

            if (includeUsers)
                query = query.Include(c => c.Users);

            var totalCount = await query.CountAsync();

            var companies = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(CompanyMapper.ToCompanyDto(includeUsers))
                .ToListAsync();

            _logger.LogInformation("📄 Listed {Count}/{Total} companies (page {Page}/{PageSize})", companies.Count, totalCount, page, pageSize);

            return Ok(new PagedResultDTO<CompanyDTO>
            {
                ImportedCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = companies
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error listing companies");
            throw;
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CompanyDTO>> GetByIdAsync(long id, [FromQuery] bool includeUsers = false)
    {
        try
        {
            var query = _context.Companies.AsNoTracking().Where(c => c.Id == id);

            if (includeUsers)
                query = query.Include(c => c.Users);

            var company = await query
                .Select(CompanyMapper.ToCompanyDto(includeUsers))
                .FirstOrDefaultAsync();

            if (company == null)
                return NotFound();

            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching company by ID {Id}", id);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDTO>> CreateAsync([FromBody] CompanyCreateDTO dto)
    {
        try
        {
            var entity = new Company
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            await _context.Companies.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = await _context.Companies.AsNoTracking()
                .Where(c => c.Id == entity.Id)
                .Select(CompanyMapper.ToCompanyDto())
                .FirstAsync();

            return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating company");
            throw;
        }
    }

    [HttpPut]
    public async Task<ActionResult<CompanyDTO>> UpdateAsync([FromBody] CompanyUpdateDTO dto)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id);
            if (company == null)
                return NotFound();

            company.Name = dto.Name.Trim();
            company.Address = dto.Address.Trim();
            company.PhoneNumber = dto.PhoneNumber.Trim();
            company.UpdatedAt = DateTime.UtcNow;
            company.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();

            var result = await _context.Companies.AsNoTracking()
                .Where(c => c.Id == dto.Id)
                .Select(CompanyMapper.ToCompanyDto())
                .FirstAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating company {CompanyId}", dto.Id);
            throw;
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
                return NotFound();

            company.IsDeleted = true;
            company.UpdatedAt = DateTime.UtcNow;
            company.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Company soft-deleted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting company {Id}", id);
            throw;
        }
    }

    [HttpPut("{id:long}/restore")]
    public async Task<IActionResult> RestoreAsync(long id)
    {
        try
        {
            var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
                return NotFound();

            company.IsDeleted = false;
            company.UpdatedAt = DateTime.UtcNow;
            company.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Company restored." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error restoring company {Id}", id);
            throw;
        }
    }

    [HttpGet("{companyId:long}/users")]
    public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsersAsync(long companyId)
    {
        try
        {
            var users = await _context.Users.AsNoTracking()
                .Where(u => u.CompanyId == companyId)
                .Include(u => u.Company)
                .Select(UserMapper.ToUserDto(true))
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching users for company {Id}", companyId);
            throw;
        }
    }

    #endregion

    #region 📥 Import/Export

    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportAsync()
    {
        try
        {
            var companies = await _context.Companies.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(CompanyMapper.ToCompanyDto())
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Name,Address,PhoneNumber");

            foreach (var c in companies)
                csv.AppendLine($"{EscapeCsv(c.Name)},{EscapeCsv(c.Address)},{EscapeCsv(c.PhoneNumber)}");

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "companies.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CSV export failed");
            throw;
        }
    }

    [HttpPost("import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<BulkOperationResultDTO<CompanyDTO>>> ImportAsync(IFormFile file)
    {
        var result = new BulkOperationResultDTO<CompanyDTO>();
        var rowCounter = 0;
        var validList = new List<Company>();

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CompanyImportMap>();

            while (await csv.ReadAsync())
            {
                rowCounter++;
                var record = csv.GetRecord<CompanyImportDTO>();

                if (string.IsNullOrWhiteSpace(record.Name))
                {
                    result.Errors.Add(new BulkOperationErrorDTO { RecordIndex = rowCounter, Message = "Name is required." });
                    continue;
                }

                validList.Add(new Company
                {
                    Name = record.Name.Trim(),
                    Address = record.Address?.Trim() ?? "",
                    PhoneNumber = record.PhoneNumber?.Trim() ?? "",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId
                });
            }

            if (validList.Any())
            {
                await _context.Companies.AddRangeAsync(validList);
                await _context.SaveChangesAsync();

                result.ImportedCount = validList.Count;
                result.Items = await _context.Companies
                    .AsNoTracking()
                    .Where(c => validList.Select(v => v.Id).Contains(c.Id))
                    .Select(CompanyMapper.ToCompanyDto())
                    .ToListAsync();
            }

            result.TotalRows = rowCounter;
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Critical CSV import failure");
            throw;
        }
    }

    #endregion

    #region 🛠️ Helpers

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    #endregion
}


///// @file CompanyController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-04-27
///// @brief Manages company CRUD operations, user access, and CSV import/export.
///// @details
///// Provides administrative endpoints for managing companies including creation,
///// update, soft delete, restore, and listing users per company. Only GlobalAdmins
///// are authorized to access this controller. CSV import/export and bulk operations
///// are also supported.
/////
///// @endpoints
///// - GET    /api/companies                    → Get paged list of companies (optional: includeUsers=true)
///// - GET    /api/companies/{id}               → Get single company by ID (optional: includeUsers=true)
///// - POST   /api/companies                    → Create a new company
///// - PUT    /api/companies                    → Update an existing company
///// - DELETE /api/companies/{id}               → Soft delete a company
///// - PUT    /api/companies/{id}/restore       → Restore a soft-deleted company
///// - GET    /api/companies/{companyId}/users  → List all users belonging to a company
///// - GET    /api/companies/export             → Export all companies as CSV
///// - POST   /api/companies/import             → Import companies from uploaded CSV


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

//[ApiController]
//[Route("api/companies")]
//[Authorize(Roles = "GlobalAdmin")]
//public class CompanyController : ControllerBase
//{
//    #region 🔒 Fields

//    private readonly AppDbContext _context;
//    private readonly ILogger<CompanyController> _logger;
//    private readonly ICurrentUserService _currentUser;
//    private const string _allowedIncludes = "Users";

//    #endregion

//    #region 🔧 Constructor

//    public CompanyController(
//        AppDbContext context,
//        ICurrentUserService currentUser,
//        ILogger<CompanyController> logger)
//    {
//        _context = context;
//        _currentUser = currentUser;
//        _logger = logger;
//    }

//    #endregion

//    #region 📄 CRUD Operations

//    [HttpGet]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<IEnumerable<CompanyDTO>>> GetCompanies(
//        [FromQuery] bool includeUsers = false,
//        [FromQuery] int page = 1,
//        [FromQuery] int pageSize = 20)
//    {
//        try
//        {
//            if (!_currentUser.IsInRole("GlobalAdmin"))
//            {
//                _logger.LogWarning("🚫 Unauthorized company list access attempt by {UserId}", _currentUser.UserId);
//                return Forbid("❌ Only GlobalAdmins can list companies.");
//            }

//            var query = _context.Companies.AsNoTracking();

//            if (includeUsers)
//                query = query.Include(c => c.Users);

//            var totalCount = await query.CountAsync();

//            var companies = await query
//                .OrderBy(c => c.Name)
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .Select(CompanyMapper.ToCompanyDto(includeUsers))
//                .ToListAsync();

//            _logger.LogInformation("📄 Listed {Count}/{Total} companies (page {Page}/{PageSize})",
//                companies.Count, totalCount, page, pageSize);

//            return Ok(new
//            {
//                Total = totalCount,
//                Page = page,
//                PageSize = pageSize,
//                Items = companies
//            });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Critical error listing companies");
//            throw;
//        }
//    }

//    [HttpGet("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<CompanyDTO>> GetCompanyById(
//        long id,
//        [FromQuery] bool includeUsers = false)
//    {
//        try
//        {
//            if (!_currentUser.IsInRole("GlobalAdmin"))
//            {
//                _logger.LogWarning("🚫 Unauthorized company access attempt for {CompanyId} by {UserId}", id, _currentUser.UserId);
//                return Forbid("❌ Only GlobalAdmins can access this company.");
//            }

//            var query = _context.Companies.AsNoTracking().Where(c => c.Id == id);

//            if (includeUsers)
//                query = query.Include(c => c.Users);

//            var company = await query
//                .Select(CompanyMapper.ToCompanyDto(includeUsers))
//                .FirstOrDefaultAsync();

//            if (company == null)
//            {
//                _logger.LogWarning("❌ Company lookup failed for ID {Id}", id);
//                return NotFound();
//            }

//            _logger.LogInformation("🔍 Retrieved company {CompanyName} (ID: {Id})", company.Name, company.Id);
//            return Ok(company);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error fetching company ID {Id}", id);
//            throw;
//        }
//    }

//    [HttpPost]
//    public async Task<IActionResult> CreateCompany([FromBody] CompanyCreateDTO dto)
//    {
//        try
//        {
//            if (!_currentUser.IsInRole("GlobalAdmin"))
//            {
//                _logger.LogWarning("🚫 Unauthorized company creation attempt by {UserId}", _currentUser.UserId);
//                return Forbid("❌ Only GlobalAdmins can create companies.");
//            }

//            var entity = new Company
//            {
//                Name = dto.Name.Trim(),
//                Address = dto.Address.Trim(),
//                PhoneNumber = dto.PhoneNumber.Trim(),
//                CreatedAt = DateTime.UtcNow,
//                CreatedBy = _currentUser.UserId
//            };

//            await _context.Companies.AddAsync(entity);
//            await _context.SaveChangesAsync();

//            var result = await _context.Companies
//                .AsNoTracking()
//                .Where(c => c.Id == entity.Id)
//                .Select(CompanyMapper.ToCompanyDto())
//                .FirstAsync();

//            _logger.LogInformation("✅ Created company '{CompanyName}' (ID: {Id}) by {UserId}",
//                entity.Name, entity.Id, _currentUser.UserId);

//            return CreatedAtAction(nameof(GetCompanyById), new { id = entity.Id }, result);
//        }
//        catch (DbUpdateException ex)
//        {
//            _logger.LogError(ex, "❌ Database error creating company '{CompanyName}'", dto.Name);
//            throw new Exception("Company creation failed due to database error");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Critical error creating company");
//            throw;
//        }
//    }

//    [HttpPut]
//    public async Task<IActionResult> UpdateCompany([FromBody] CompanyUpdateDTO dto)
//    {
//        try
//        {
//            if (!_currentUser.IsInRole("GlobalAdmin"))
//            {
//                _logger.LogWarning("🚫 Unauthorized company update attempt for {CompanyId} by {UserId}",
//                    dto.Id, _currentUser.UserId);
//                return Forbid("❌ Only GlobalAdmins can update companies.");
//            }

//            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id);
//            if (company == null)
//            {
//                _logger.LogWarning("❌ Update failed – company not found: {Id}", dto.Id);
//                return NotFound();
//            }

//            company.Name = dto.Name.Trim();
//            company.Address = dto.Address.Trim();
//            company.PhoneNumber = dto.PhoneNumber.Trim();
//            company.UpdatedAt = DateTime.UtcNow;
//            company.UpdatedBy = _currentUser.UserId;

//            await _context.SaveChangesAsync();

//            var result = await _context.Companies
//                .AsNoTracking()
//                .Where(c => c.Id == dto.Id)
//                .Select(CompanyMapper.ToCompanyDto())
//                .FirstAsync();

//            _logger.LogInformation("✏️ Updated company '{CompanyName}' (ID: {Id}) by {UserId}",
//                company.Name, company.Id, _currentUser.UserId);

//            return Ok(result);
//        }
//        catch (DbUpdateException ex)
//        {
//            _logger.LogError(ex, "❌ Database error updating company {CompanyId}", dto.Id);
//            throw;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error updating company {CompanyId}", dto.Id);
//            throw;
//        }
//    }

//    [HttpDelete("{id:long}")]
//    public async Task<IActionResult> DeleteCompany(long id)
//    {
//        try
//        {
//            if (!_currentUser.IsInRole("GlobalAdmin"))
//            {
//                _logger.LogWarning("🚫 Unauthorized delete attempt for company {Id} by {UserId}",
//                    id, _currentUser.UserId);
//                return Forbid("❌ Only GlobalAdmins can delete companies.");
//            }

//            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
//            if (company == null)
//            {
//                _logger.LogWarning("❌ Delete failed – company not found: {Id}", id);
//                return NotFound();
//            }

//            if (company.IsDeleted)
//            {
//                _logger.LogWarning("⚠️ Duplicate delete attempt for company {Id}", id);
//                return BadRequest("⚠️ Company is already deleted.");
//            }

//            company.IsDeleted = true;
//            company.UpdatedAt = DateTime.UtcNow;
//            company.UpdatedBy = _currentUser.UserId;

//            await _context.SaveChangesAsync();

//            _logger.LogWarning("🗑️ Soft-deleted company '{CompanyName}' (ID: {Id}) by {UserId}",
//                company.Name, company.Id, _currentUser.UserId);

//            return Ok(new { message = "✅ Company soft-deleted." });
//        }
//        catch (DbUpdateException ex)
//        {
//            _logger.LogError(ex, "❌ Database error deleting company {Id}", id);
//            throw;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error deleting company {Id}", id);
//            throw;
//        }
//    }

//    [HttpPut("{id:long}/restore")]
//    public async Task<IActionResult> RestoreCompany(long id)
//    {
//        try
//        {
//            if (!_currentUser.IsInRole("GlobalAdmin"))
//            {
//                _logger.LogWarning("🚫 Unauthorized restore attempt for company {Id} by {UserId}",
//                    id, _currentUser.UserId);
//                return Forbid("❌ Only GlobalAdmins can restore companies.");
//            }

//            var company = await _context.Companies.IgnoreQueryFilters()
//                .FirstOrDefaultAsync(c => c.Id == id);

//            if (company == null)
//            {
//                _logger.LogWarning("❌ Restore failed – company not found: {Id}", id);
//                return NotFound();
//            }

//            if (!company.IsDeleted)
//            {
//                _logger.LogInformation("ℹ️ Restore skipped – company {Id} not deleted", id);
//                return BadRequest("ℹ️ Company is not deleted.");
//            }

//            company.IsDeleted = false;
//            company.UpdatedAt = DateTime.UtcNow;
//            company.UpdatedBy = _currentUser.UserId;

//            await _context.SaveChangesAsync();

//            _logger.LogInformation("♻️ Restored company '{CompanyName}' (ID: {Id}) by {UserId}",
//                company.Name, company.Id, _currentUser.UserId);

//            return Ok(new { message = "✅ Company restored." });
//        }
//        catch (DbUpdateException ex)
//        {
//            _logger.LogError(ex, "❌ Database error restoring company {Id}", id);
//            throw;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error restoring company {Id}", id);
//            throw;
//        }
//    }

//    [HttpGet("{companyId:long}/users")]
//    public async Task<ActionResult<IEnumerable<UserDTO>>> GetCompanyUsers(long companyId)
//    {
//        try
//        {
//            if (!_currentUser.IsInRole("GlobalAdmin"))
//            {
//                _logger.LogWarning("🚫 Unauthorized user list access for company {Id} by {UserId}",
//                    companyId, _currentUser.UserId);
//                return Forbid("❌ Only GlobalAdmins can access this data.");
//            }

//            var users = await _context.Users
//                .AsNoTracking()
//                .Where(u => u.CompanyId == companyId)
//                .Include(u => u.Company)
//                .Select(UserMapper.ToUserDto(true))
//                .ToListAsync();

//            _logger.LogInformation("👥 Retrieved {Count} users for company {CompanyId}", users.Count, companyId);

//            return Ok(users);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error fetching users for company {Id}", companyId);
//            throw;
//        }
//    }

//    #endregion

//    #region 📥 Import/Export

//    [HttpGet("export")]
//    [Produces("text/csv")]
//    public async Task<IActionResult> ExportCompanies()
//    {
//        try
//        {
//            var companies = await _context.Companies
//                .AsNoTracking()
//                .OrderBy(c => c.Name)
//                .Select(CompanyMapper.ToCompanyDto())
//                .ToListAsync();

//            var csv = new StringBuilder();
//            csv.AppendLine("Name,Address,PhoneNumber");

//            foreach (var c in companies)
//                csv.AppendLine($"{EscapeCsv(c.Name)},{EscapeCsv(c.Address)},{EscapeCsv(c.PhoneNumber)}");

//            _logger.LogInformation("📤 Exported {Count} companies as CSV", companies.Count);

//            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "companies.csv");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ CSV export failed");
//            throw;
//        }
//    }

//    [HttpPost("import")]
//    [RequestSizeLimit(5 * 1024 * 1024)]
//    public async Task<ActionResult<BulkOperationResultDTO<CompanyDTO>>> ImportCompanies(IFormFile file)
//    {
//        var result = new BulkOperationResultDTO<CompanyDTO>();
//        var rowCounter = 0;
//        var validList = new List<Company>();

//        try
//        {
//            using var reader = new StreamReader(file.OpenReadStream());
//            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
//            csv.Context.RegisterClassMap<CompanyImportMap>();

//            while (await csv.ReadAsync())
//            {
//                rowCounter++;
//                try
//                {
//                    var record = csv.GetRecord<CompanyImportDTO>();
//                    if (string.IsNullOrWhiteSpace(record.Name))
//                    {
//                        result.Errors.Add(new BulkOperationErrorDTO
//                        {
//                            RecordIndex = rowCounter,
//                            Message = "Name is required."
//                        });
//                        _logger.LogWarning("📥 CSV import: Missing name in row {Row}", rowCounter);
//                        continue;
//                    }

//                    var entity = new Company
//                    {
//                        Name = record.Name.Trim(),
//                        Address = record.Address?.Trim() ?? "",
//                        PhoneNumber = record.PhoneNumber?.Trim() ?? "",
//                        CreatedAt = DateTime.UtcNow,
//                        CreatedBy = _currentUser.UserId
//                    };

//                    validList.Add(entity);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ CSV import error in row {Row}", rowCounter);
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = $"CSV error: {ex.Message}"
//                    });
//                }
//            }

//            if (validList.Any())
//            {
//                await _context.Companies.AddRangeAsync(validList);
//                await _context.SaveChangesAsync();

//                result.ImportedCount = validList.Count;
//                result.Items = await _context.Companies
//                    .AsNoTracking()
//                    .Where(c => validList.Select(v => v.Id).Contains(c.Id))
//                    .Select(CompanyMapper.ToCompanyDto())
//                    .ToListAsync();
//            }

//            result.TotalRows = rowCounter;

//            _logger.LogInformation("📥 Import completed: {Success}/{Total} with {Errors} errors",
//                result.ImportedCount, rowCounter, result.Errors.Count);

//            return Ok(result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Critical CSV import failure");
//            throw;
//        }
//    }

//    #endregion

//    #region 🛠️ Helpers

//    private static string EscapeCsv(string value) =>
//        $"\"{value.Replace("\"", "\"\"")}\"";

//    #endregion
//}




///// @file CompanyController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-04-27
///// @brief Manages company CRUD operations, user access, and CSV import/export.
///// @details
///// This controller allows GlobalAdmins to manage companies. Features include:
///// listing, creation, update, soft delete, restore, user listing per company,
///// CSV export, and CSV import with bulk error handling.
/////
///// @endpoints
///// - GET /api/companies → List companies (paged)
///// - GET /api/companies/{id} → Get company by ID
///// - POST /api/companies → Create new company
///// - PUT /api/companies → Update existing company
///// - DELETE /api/companies/{id} → Soft delete company
///// - PUT /api/companies/{id}/restore → Restore soft-deleted company
///// - GET /api/companies/{companyId}/users → Get all users in a company
///// - GET /api/companies/export → Export companies as CSV
///// - POST /api/companies/import → Import companies from CSV

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
///// 🏢 Controller for managing companies. Only accessible to GlobalAdmins.
///// </summary>
//[ApiController]
//[Route("api/companies")]
//[Authorize(Roles = "GlobalAdmin")]
//public class CompanyController : ControllerBase
//{
//    #region 🔒 Fields

//    private readonly AppDbContext _context; // 🧱 EF Core database context
//    private readonly ILogger<CompanyController> _logger; // 🧾 Logger for audit/logging
//    private readonly ICurrentUserService _currentUser; // 👤 Current user info provider
//    private const string _allowedIncludes = "Users"; // 🧩 Valid include filters

//    #endregion

//    #region 🔧 Constructor

//    /// <summary>
//    /// 🧱 Constructor injecting database, logger, and current user service.
//    /// </summary>
//    public CompanyController(AppDbContext context, ICurrentUserService currentUser, ILogger<CompanyController> logger)
//    {
//        _context = context;
//        _currentUser = currentUser;
//        _logger = logger;
//    }

//    #endregion

//    #region 📄 CRUD Operations

//    /// <summary>
//    /// 📄 Returns a paginated list of all companies.
//    /// </summary>
//    [HttpGet]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<IEnumerable<CompanyDTO>>> GetCompanies(
//        [FromQuery] bool includeUsers = false,
//        [FromQuery] int page = 1,
//        [FromQuery] int pageSize = 20)
//    {
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid("❌ Only GlobalAdmins can list companies.");

//        var query = _context.Companies.AsNoTracking();

//        if (includeUsers)
//            query = query.Include(c => c.Users);

//        var ImportedCount = await query.CountAsync();

//        var companies = await query
//            .OrderBy(c => c.Name)
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .Select(CompanyMapper.ToCompanyDto(includeUsers))
//            .ToListAsync();

//        _logger.LogInformation("📄 Listed {Count} companies (page {Page}/{PageSize})", companies.Count, page, pageSize);

//        return Ok(new
//        {
//            Total = ImportedCount,
//            Page = page,
//            PageSize = pageSize,
//            Items = companies
//        });
//    }

//    /// <summary>
//    /// 🔍 Returns a specific company by ID.
//    /// </summary>
//    [HttpGet("{id:long}")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<ActionResult<CompanyDTO>> GetCompanyById(long id, [FromQuery] bool includeUsers = false)
//    {
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid("❌ Only GlobalAdmins can access this company.");

//        var query = _context.Companies.AsNoTracking().Where(c => c.Id == id);

//        if (includeUsers)
//            query = query.Include(c => c.Users);

//        var company = await query
//            .Select(CompanyMapper.ToCompanyDto(includeUsers))
//            .FirstOrDefaultAsync();

//        if (company == null)
//        {
//            _logger.LogWarning("❌ Company not found with ID {Id}", id);
//            return NotFound();
//        }

//        _logger.LogInformation("🔍 Fetched company {Name} ({Id})", company.Name, company.Id);

//        return Ok(company);
//    }

//    /// <summary>
//    /// ➕ Creates a new company.
//    /// </summary>
//    [HttpPost]
//    public async Task<IActionResult> CreateCompany([FromBody] CompanyCreateDTO dto)
//    {
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid("❌ Only GlobalAdmins can create companies.");

//        var entity = new Company
//        {
//            Name = dto.Name.Trim(),
//            Address = dto.Address.Trim(),
//            PhoneNumber = dto.PhoneNumber.Trim(),
//            CreatedAt = DateTime.UtcNow,
//            CreatedBy = _currentUser.UserId
//        };

//        _context.Companies.Add(entity);
//        await _context.SaveChangesAsync();

//        var result = await _context.Companies
//            .AsNoTracking()
//            .Where(c => c.Id == entity.Id)
//            .Select(CompanyMapper.ToCompanyDto())
//            .FirstAsync();

//        _logger.LogInformation("🏢 Created company '{Name}' (ID: {Id}) by user {UserId}", entity.Name, entity.Id, _currentUser.UserId);

//        return CreatedAtAction(nameof(GetCompanyById), new { id = entity.Id }, result);
//    }

//    /// <summary>
//    /// ✏️ Updates an existing company.
//    /// </summary>
//    [HttpPut]
//    public async Task<IActionResult> UpdateCompany([FromBody] CompanyUpdateDTO dto)
//    {
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid("❌ Only GlobalAdmins can update companies.");

//        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id);
//        if (company == null)
//        {
//            _logger.LogWarning("❌ Update failed – company not found: {Id}", dto.Id);
//            return NotFound();
//        }

//        company.Name = dto.Name.Trim();
//        company.Address = dto.Address.Trim();
//        company.PhoneNumber = dto.PhoneNumber.Trim();
//        company.UpdatedAt = DateTime.UtcNow;
//        company.UpdatedBy = _currentUser.UserId;

//        await _context.SaveChangesAsync();

//        var result = await _context.Companies
//            .AsNoTracking()
//            .Where(c => c.Id == dto.Id)
//            .Select(CompanyMapper.ToCompanyDto())
//            .FirstAsync();

//        _logger.LogInformation("✏️ Updated company '{Name}' (ID: {Id}) by user {UserId}", company.Name, company.Id, _currentUser.UserId);

//        return Ok(result);
//    }

//    /// <summary>
//    /// 🗑️ Soft-deletes a company.
//    /// </summary>
//    [HttpDelete("{id:long}")]
//    public async Task<IActionResult> DeleteCompany(long id)
//    {
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid("❌ Only GlobalAdmins can delete companies.");

//        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
//        if (company == null)
//        {
//            _logger.LogWarning("❌ Delete failed – company not found: {Id}", id);
//            return NotFound();
//        }

//        if (company.IsDeleted)
//        {
//            _logger.LogWarning("⚠️ Company already deleted: {Id}", id);
//            return BadRequest("⚠️ Company is already deleted.");
//        }

//        company.IsDeleted = true;
//        company.UpdatedAt = DateTime.UtcNow;
//        company.UpdatedBy = _currentUser.UserId;

//        await _context.SaveChangesAsync();

//        _logger.LogWarning("🗑️ Soft-deleted company '{Name}' (ID: {Id}) by user {UserId}", company.Name, company.Id, _currentUser.UserId);

//        return Ok(new { message = "✅ Company soft-deleted." });
//    }

//    /// <summary>
//    /// ♻️ Restores a soft-deleted company.
//    /// </summary>
//    [HttpPut("{id:long}/restore")]
//    public async Task<IActionResult> RestoreCompany(long id)
//    {
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid("❌ Only GlobalAdmins can restore companies.");

//        var company = await _context.Companies.IgnoreQueryFilters()
//            .FirstOrDefaultAsync(c => c.Id == id);

//        if (company == null)
//        {
//            _logger.LogWarning("❌ Restore failed – company not found: {Id}", id);
//            return NotFound();
//        }

//        if (!company.IsDeleted)
//        {
//            _logger.LogWarning("ℹ️ Restore skipped – company not deleted: {Id}", id);
//            return BadRequest("ℹ️ Company is not deleted.");
//        }

//        company.IsDeleted = false;
//        company.UpdatedAt = DateTime.UtcNow;
//        company.UpdatedBy = _currentUser.UserId;

//        await _context.SaveChangesAsync();

//        _logger.LogInformation("♻️ Restored company '{Name}' (ID: {Id}) by user {UserId}", company.Name, company.Id, _currentUser.UserId);

//        return Ok(new { message = "✅ Company restored." });
//    }

//    /// <summary>
//    /// 👥 Lists all users of a specific company.
//    /// </summary>
//    [HttpGet("{companyId:long}/users")]
//    public async Task<ActionResult<IEnumerable<UserDTO>>> GetCompanyUsers(long companyId)
//    {
//        if (!_currentUser.IsInRole("GlobalAdmin"))
//            return Forbid("❌ Only GlobalAdmins can access this data.");

//        var users = await _context.Users
//            .AsNoTracking()
//            .Where(u => u.CompanyId == companyId)
//            .Include(u => u.Company)
//            .Select(UserMapper.ToUserDto(true))
//            .ToListAsync();

//        _logger.LogInformation("👥 Loaded {Count} users for company ID {CompanyId}", users.Count, companyId);

//        return Ok(users);
//    }

//    #endregion

//    #region 📥 Import/Export

//    /// <summary>
//    /// 📤 Exports all companies as CSV.
//    /// </summary>
//    [HttpGet("export")]
//    [Produces("text/csv")]
//    public async Task<IActionResult> ExportCompanies()
//    {
//        var companies = await _context.Companies
//            .AsNoTracking()
//            .OrderBy(c => c.Name)
//            .Select(CompanyMapper.ToCompanyDto())
//            .ToListAsync();

//        var csv = new StringBuilder();
//        csv.AppendLine("Name,Address,PhoneNumber");

//        foreach (var c in companies)
//            csv.AppendLine($"{c.Name},{c.Address},{c.PhoneNumber}");

//        _logger.LogInformation("📤 Exported {Count} companies as CSV", companies.Count);

//        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "companies.csv");
//    }

//    /// <summary>
//    /// 📥 Imports companies from a CSV file.
//    /// </summary>
//    [HttpPost("import")]
//    [RequestSizeLimit(5 * 1024 * 1024)]
//    public async Task<ActionResult<BulkOperationResultDTO<CompanyDTO>>> ImportCompanies(IFormFile file)
//    {
//        var result = new BulkOperationResultDTO<CompanyDTO>();
//        var rowCounter = 0;
//        var validList = new List<Company>();

//        using var reader = new StreamReader(file.OpenReadStream());
//        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
//        csv.Context.RegisterClassMap<CompanyImportMap>();

//        while (await csv.ReadAsync())
//        {
//            rowCounter++;
//            try
//            {
//                var record = csv.GetRecord<CompanyImportDTO>();
//                if (string.IsNullOrWhiteSpace(record.Name))
//                {
//                    result.Errors.Add(new BulkOperationErrorDTO
//                    {
//                        RecordIndex = rowCounter,
//                        Message = "Name is required."
//                    });
//                    continue;
//                }

//                var entity = new Company
//                {
//                    Name = record.Name.Trim(),
//                    Address = record.Address?.Trim() ?? "",
//                    PhoneNumber = record.PhoneNumber?.Trim() ?? "",
//                    CreatedAt = DateTime.UtcNow,
//                    CreatedBy = _currentUser.UserId
//                };

//                validList.Add(entity);
//            }
//            catch (Exception ex)
//            {
//                result.Errors.Add(new BulkOperationErrorDTO
//                {
//                    RecordIndex = rowCounter,
//                    Message = $"CSV error: {ex.Message}"
//                });
//            }
//        }

//        if (validList.Any())
//        {
//            _context.Companies.AddRange(validList);
//            await _context.SaveChangesAsync();

//            result.ImportedCount = validList.Count;
//            result.Items = await _context.Companies
//                .AsNoTracking()
//                .Where(c => validList.Select(v => v.Id).Contains(c.Id))
//                .Select(CompanyMapper.ToCompanyDto())
//                .ToListAsync();
//        }

//        result.TotalRows = rowCounter;

//        _logger.LogInformation("📥 Imported {Count} companies with {ErrorCount} errors", result.ImportedCount, result.Errors.Count);

//        return Ok(result);
//    }

//    #endregion
//}



