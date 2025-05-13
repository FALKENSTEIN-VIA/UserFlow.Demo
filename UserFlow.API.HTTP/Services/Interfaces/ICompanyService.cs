/// *****************************************************************************************
/// @file ICompanyService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for company-related operations in the UserFlow API.
/// @details
/// Provides methods to create, update, delete, restore, import/export, and register companies.
/// Supports paginated queries and bulk creation for admin workflows.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 🏢 Interface defining all company-related operations in the UserFlow system.
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// 📄 Gets a list of all companies.
    /// </summary>
    /// <returns>List of <see cref="CompanyDTO"/>.</returns>
    Task<IEnumerable<CompanyDTO>> GetAllAsync();

    /// <summary>
    /// 🔍 Retrieves a single company by its unique ID.
    /// </summary>
    /// <param name="id">Company ID to retrieve.</param>
    /// <returns>Matching <see cref="CompanyDTO"/> or null if not found.</returns>
    Task<CompanyDTO?> GetByIdAsync(long id);

    /// <summary>
    /// 🆕 Creates a new company in the system.
    /// </summary>
    /// <param name="dto">Company creation data.</param>
    /// <returns>Created <see cref="CompanyDTO"/>.</returns>
    Task<CompanyDTO> CreateCompanyAsync(CompanyCreateDTO dto);

    /// <summary>
    /// 📝 Updates an existing company.
    /// </summary>
    /// <param name="id">ID of the company to update.</param>
    /// <param name="dto">Updated data.</param>
    /// <returns>True if update succeeded.</returns>
    Task<bool> UpdateCompanyAsync(long id, CompanyUpdateDTO dto);

    /// <summary>
    /// 🗑️ Deletes a company (soft delete).
    /// </summary>
    /// <param name="id">ID of the company to delete.</param>
    /// <returns>True if deletion succeeded.</returns>
    Task<bool> DeleteCompanyAsync(long id);

    /// <summary>
    /// ♻️ Restores a previously deleted company.
    /// </summary>
    /// <param name="id">ID of the company to restore.</param>
    /// <returns>Restored <see cref="CompanyDTO"/> or null.</returns>
    Task<CompanyDTO?> RestoreCompanyAsync(long id);

    /// <summary>
    /// 📦 Bulk creates multiple companies at once.
    /// </summary>
    /// <param name="companies">List of companies to create.</param>
    /// <returns>Import result with details.</returns>
    Task<BulkOperationResultDTO<CompanyDTO>> BulkCreateCompaniesAsync(List<CompanyCreateDTO> companies);

    /// <summary>
    /// 📃 Retrieves a paginated list of companies.
    /// </summary>
    /// <param name="page">Current page index.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <returns>Paged result set of companies.</returns>
    Task<PagedResultDTO<CompanyDTO>> GetPagedCompaniesAsync(int page, int pageSize);

    /// <summary>
    /// 📥 Imports companies from a file (CSV or Excel).
    /// </summary>
    /// <param name="file">Uploaded file containing company data.</param>
    /// <returns>Import result with errors and success count.</returns>
    Task<BulkOperationResultDTO<CompanyDTO>> ImportCompaniesAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all companies as downloadable file (CSV).
    /// </summary>
    /// <returns>Stream containing export file.</returns>
    Task<Stream> ExportCompaniesAsync();

    /// <summary>
    /// 🏢 Registers a company and its first admin user.
    /// </summary>
    /// <param name="dto">Company and admin registration data.</param>
    /// <returns>Authentication tokens and user info if successful.</returns>
    Task<AuthResponseDTO?> RegisterCompanyAsync(CompanyRegisterDTO dto);
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - Supports full CRUD, restore, paging, and bulk operations.
/// - 📁 CSV-based import/export is supported via <see cref="IFormFile"/>.
/// - 🏢 Registration returns tokens for immediate login after company creation.
/// - 🔐 Access is secured via `AuthorizedHttpClient`.
/// </remarks>
