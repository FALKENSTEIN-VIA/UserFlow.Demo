/// *****************************************************************************************
/// @file IEmployeeService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Interface for managing employee records and performing bulk import/export.
/// @details
/// This interface defines methods for retrieving, creating, deleting, restoring, 
/// and managing employee entities, including bulk operations and CSV import/export functionality.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👨‍💼 Interface for handling employee-related operations (CRUD, import/export, bulk).
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// 📄 Retrieves all employees available to the current company context.
    /// </summary>
    /// <returns>A list of <see cref="EmployeeDTO"/> objects.</returns>
    Task<IEnumerable<EmployeeDTO>> GetAllAsync();

    /// <summary>
    /// 🔍 Retrieves a specific employee by their ID.
    /// </summary>
    /// <param name="id">The employee's unique identifier.</param>
    /// <returns><see cref="EmployeeDTO"/> if found, otherwise null.</returns>
    Task<EmployeeDTO?> GetByIdAsync(long id);

    /// <summary>
    /// 🔍 Retrieves employees by their company ID.
    /// </summary>
    /// <param name="id">The company's unique identifier.</param>
    /// <returns><see cref="EmployeeDTO"/> if found, otherwise null.</returns>
    Task<IEnumerable<EmployeeDTO>> GetEmployeesByCompanyIdAsync(long companyId);

    /// <summary>
    /// 🆕 Creates a new employee with the provided data.
    /// </summary>
    /// <param name="dto">The DTO containing the employee's data.</param>
    /// <returns>The created <see cref="EmployeeDTO"/> or null on failure.</returns>
    Task<EmployeeDTO?> CreateAsync(EmployeeCreateDTO dto);

    /// <summary>
    /// 🗑️ Soft-deletes an employee by ID.
    /// </summary>
    /// <param name="id">The ID of the employee to delete.</param>
    /// <returns>True if deletion succeeded, false otherwise.</returns>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// ♻️ Restores a previously soft-deleted employee.
    /// </summary>
    /// <param name="id">The ID of the employee to restore.</param>
    /// <returns>The restored <see cref="EmployeeDTO"/> or null.</returns>
    Task<EmployeeDTO?> RestoreAsync(long id);

    /// <summary>
    /// 📦 Bulk creates multiple employees using a bulk DTO.
    /// </summary>
    /// <param name="bulkDto">The bulk creation DTO.</param>
    /// <returns><see cref="BulkOperationResultDTO{EmployeeDTO}"/> with details of successes and failures.</returns>
    Task<BulkOperationResultDTO<EmployeeDTO>?> BulkCreateAsync(BulkEmployeeCreateDTO bulkDto);

    /// <summary>
    /// 📃 Retrieves paginated employee results.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <returns>Paged result of <see cref="EmployeeDTO"/>.</returns>
    Task<PagedResultDTO<EmployeeDTO>?> GetPagedAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 📥 Imports employee data from a CSV file.
    /// </summary>
    /// <param name="file">CSV file containing employee data.</param>
    /// <returns>Bulk operation result or null if the import failed.</returns>
    Task<BulkOperationResultDTO<EmployeeDTO>?> ImportAsync(IFormFile file);

    /// <summary>
    /// 📤 Exports all employees to a downloadable CSV file.
    /// </summary>
    /// <returns>A stream containing the exported employee data.</returns>
    Task<Stream?> ExportAsync();
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 📥 Import and 📤 export support standard CSV formats with header validation.
/// - ✅ Soft delete and restore functionality supports safe data recovery.
/// - 📊 Paging allows responsive UI for large datasets.
/// - ⚡ Use `BulkCreateAsync` for efficient batch operations.
/// - 🔐 Role-based access must be enforced in implementations.
/// - ⚠ Always validate data before processing bulk imports or uploads.
/// </remarks>
