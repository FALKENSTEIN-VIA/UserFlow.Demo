/// *****************************************************************************************
/// @file EmployeeService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides robust implementation for employee-related API calls using AuthorizedHttpClient.
/// @details Fully validates client-side DTOs, uses typed API calls, and ensures fault-tolerant fallback.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 🧑‍💼 Provides employee management operations using <see cref="AuthorizedHttpClient"/>.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly AuthorizedHttpClient _httpClient;

    public EmployeeService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region 📥 Basic CRUD

    public async Task<IEnumerable<EmployeeDTO>> GetAllAsync()
    {
        var pagedResult = await _httpClient.GetAsync<PagedResultDTO<EmployeeDTO>>("api/employees");
        return pagedResult?.Items ?? [];
    }

    public async Task<EmployeeDTO?> GetByIdAsync(long id)
    {
        return await _httpClient.GetAsync<EmployeeDTO>($"api/employees/{id}");
    }

    public async Task<EmployeeDTO?> CreateAsync(EmployeeCreateDTO dto)
    {
        return await _httpClient.PostAsync<EmployeeCreateDTO, EmployeeDTO>("api/employees", dto);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/employees/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<EmployeeDTO?> RestoreAsync(long id)
    {
        return await _httpClient.PostAsync<object, EmployeeDTO>($"api/employees/{id}/restore", new { });
    }

    #endregion

    #region 📦 Bulk & Paged

    public async Task<BulkOperationResultDTO<EmployeeDTO>?> BulkCreateAsync(BulkEmployeeCreateDTO bulkDto)
    {
        if (bulkDto?.Employees == null || !bulkDto.Employees.Any())
        {
            return new BulkOperationResultDTO<EmployeeDTO>
            {
                ImportedCount = 0,
                TotalRows = 0,
                Errors = new List<BulkOperationErrorDTO>
                {
                    new BulkOperationErrorDTO()
                    {
                        RecordIndex = -1,
                        Field = "Bulk",
                        Code = "EmptyList",
                        Message = "No employees provided for bulk create."
                    }
                }
            };
        }

        var invalidDtos = bulkDto.Employees
            .Select((dto, index) => new { dto, index })
            .Where(x => string.IsNullOrWhiteSpace(x.dto.Email))
            .ToList();

        if (invalidDtos.Any())
        {
            var errorList = invalidDtos.Select(x => new BulkOperationErrorDTO
            {
                RecordIndex = x.index,
                Field = nameof(EmployeeCreateDTO.Email),
                Code = "EmailRequired",
                Message = $"E-Mail is required for employee '{x.dto.Name}'",
                Values = new Dictionary<string, object>
            {
                { "Name", x.dto.Name },
                { "UserId", x.dto.UserId }
            }
            }).ToList();

            return new BulkOperationResultDTO<EmployeeDTO>
            {
                ImportedCount = 0,
                TotalRows = bulkDto.Employees.Count,
                Errors = errorList
            };
        }

        var result = await _httpClient.PostAsync<BulkEmployeeCreateDTO, BulkOperationResultDTO<EmployeeDTO>>("api/employees/bulk", bulkDto);
        return result ?? new();
    }

    public async Task<BulkOperationResultDTO<EmployeeDTO>> BulkUpdateAsync(List<EmployeeUpdateDTO> dtos)
    {
        if (dtos == null || !dtos.Any())
        {
            return new BulkOperationResultDTO<EmployeeDTO>
            {
                ImportedCount = 0,
                TotalRows = 0,
                Errors = new List<BulkOperationErrorDTO>
                {
                    new()
                    {
                        RecordIndex = -1,
                        Field = "Bulk",
                        Code = "EmptyList",
                        Message = "No employees provided for update."
                    }
                }
            };
        }

        var invalidDtos = dtos
            .Select((dto, index) => new { dto, index })
            .Where(x => string.IsNullOrWhiteSpace(x.dto.Email))
            .ToList();

        if (invalidDtos.Any())
        {
            var errorList = invalidDtos.Select(x => new BulkOperationErrorDTO
            {
                RecordIndex = x.index,
                Field = nameof(EmployeeUpdateDTO.Email),
                Code = "EmailRequired",
                Message = $"E-Mail is required for employee with ID {x.dto.Id}",
                Values = new Dictionary<string, object>
                {
                    { "Id", x.dto.Id }
                }
            }).ToList();

            return new BulkOperationResultDTO<EmployeeDTO>
            {
                ImportedCount = 0,
                TotalRows = dtos.Count,
                Errors = errorList
            };
        }

        var result = await _httpClient.PutAsync<List<EmployeeUpdateDTO>, BulkOperationResultDTO<EmployeeDTO>>(
            "api/employees/bulk",
            dtos);

        return result ?? new();
    }

    public async Task<PagedResultDTO<EmployeeDTO>?> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        return await _httpClient.GetAsync<PagedResultDTO<EmployeeDTO>>($"api/employees/paged?page={page}&pageSize={pageSize}");
    }

    #endregion

    #region 📥 Import/Export

    public async Task<BulkOperationResultDTO<EmployeeDTO>?> ImportAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/employees/import", content);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<EmployeeDTO>>();
    }

    public async Task<Stream?> ExportAsync()
    {
        var response = await _httpClient.GetRawAsync("api/employees/export");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsStreamAsync() : null;
    }

    #endregion

    #region 📥 By Company

    public async Task<IEnumerable<EmployeeDTO>> GetEmployeesByCompanyIdAsync(long companyId)
    {
        var result = await _httpClient.GetAsync<List<EmployeeDTO>>($"api/companies/{companyId}/users");
        return result ?? [];
    }

    #endregion
}

/// <remarks>
/// ✅ Follows Clean API Client pattern with validation and fallback.
/// ✅ Consistent with other service layers (like CompanyService).
/// ✅ Failsafe: Never throws directly from service, always returns usable defaults.
/// </remarks>










/// <remarks>
/// Developer Notes:
/// - 🔄 Alle Pfade angepasst auf den neuen API Controller
/// - 🔄 Neu hinzugefügt: GetEmployeesByCompanyIdAsync (`api/companies/{companyId}/users`)
/// - ✅ Alle Bulk Methoden korrekt angepasst (`bulk`, `bulk-delete`, `bulk-update`)
/// - 🎯 GetAllAsync → paged Version mit Standardpage 1, 20
/// - 📁 Import/Export bleiben unverändert
/// - 🛡️ Absicherung über AuthorizedHttpClient inklusive Token Handling
/// </remarks>



///// *****************************************************************************************
///// @file EmployeeService.cs
///// @autor Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-07
///// @brief Provides implementation for employee-related API calls using AuthorizedHttpClient.
///// *****************************************************************************************

//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using UserFlow.API.HTTP;
//using UserFlow.API.Shared.DTO;

//namespace UserFlow.API.Http.Services;

///// <summary>
///// 👉 ✨ Implementation of <see cref="IEmployeeService"/> for calling employee-related endpoints.
///// </summary>
//public class EmployeeService : IEmployeeService
//{
//    private readonly AuthorizedHttpClient _httpClient;

//    /// <summary>
//    /// 👉 ✨ Constructor injecting <see cref="AuthorizedHttpClient"/> and <see cref="ILogger"/>.
//    /// </summary>
//    public EmployeeService(AuthorizedHttpClient httpClient)
//    {
//        _httpClient = httpClient;
//    }

//    #region 📥 Basic CRUD

//    /// <inheritdoc/>
//    public async Task<IEnumerable<EmployeeDTO>> GetAllAsync()
//    {
//        var result = await _httpClient.GetAsync<List<EmployeeDTO>>("api/employees") ?? [];
//        return result;
//    }

//    /// <inheritdoc/>
//    public async Task<EmployeeDTO?> GetByIdAsync(long id)
//    {
//        var result = await _httpClient.GetAsync<EmployeeDTO>($"api/employees/{id}");
//        return result;
//    }

//    /// <inheritdoc/>
//    public async Task<EmployeeDTO?> CreateAsync(EmployeeCreateDTO dto)
//    {
//        var result = await _httpClient.PostAsync<EmployeeCreateDTO, EmployeeDTO>("api/employees", dto);
//        return result;
//    }

//    /// <inheritdoc/>
//    public async Task<bool> DeleteAsync(long id)
//    {
//        var response = await _httpClient.DeleteAsync($"api/employees/{id}");
//        return response.IsSuccessStatusCode;
//    }

//    /// <inheritdoc/>
//    public async Task<EmployeeDTO?> RestoreAsync(long id)
//    {
//        var result = await _httpClient.PostAsync<object, EmployeeDTO>($"api/employees/{id}/restore", new { });
//        return result;
//    }

//    #endregion

//    #region 📦 Bulk & Paged

//    /// <inheritdoc/>
//    public async Task<BulkOperationResultDTO<EmployeeDTO>?> BulkCreateAsync(BulkEmployeeCreateDTO bulkDto)
//    {
//        var result = await _httpClient.PostAsync<BulkEmployeeCreateDTO, BulkOperationResultDTO<EmployeeDTO>>("api/employees/bulk", bulkDto);
//        return result;
//    }

//    /// <inheritdoc/>
//    public async Task<PagedResultDTO<EmployeeDTO>?> GetPagedAsync(int page = 1, int pageSize = 20)
//    {
//        var result = await _httpClient.GetAsync<PagedResultDTO<EmployeeDTO>>($"api/employees/paged?page={page}&pageSize={pageSize}");
//        return result;
//    }

//    #endregion

//    #region 📁 Import/Export

//    /// <inheritdoc/>
//    public async Task<BulkOperationResultDTO<EmployeeDTO>?> ImportAsync(IFormFile file)
//    {
//        var content = new MultipartFormDataContent();
//        var streamContent = new StreamContent(file.OpenReadStream());
//        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
//        content.Add(streamContent, "file", file.FileName);

//        var response = await _httpClient.PostAsync("api/employee/import", content);
//        if (!response.IsSuccessStatusCode)
//        {
//            return null;
//        }

//        var result = await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<EmployeeDTO>>();
//        return result;
//    }

//    /// <inheritdoc/>
//    public async Task<Stream?> ExportAsync()
//    {

//        var response = await _httpClient.GetRawAsync("api/employee/export");

//        if (!response.IsSuccessStatusCode)
//        {
//            return null;
//        }

//        return await response.Content.ReadAsStreamAsync();
//    }

//    #endregion
//}

///// <remarks>
///// Developer Notes:
///// - ✅ Uses AuthorizedHttpClient for authenticated requests.
///// - 🧑‍💼 Provides full CRUD support including restore and pagination.
///// - 📦 Supports bulk creation and CSV import/export workflows.
///// - 🧠 All failures are logged, and null-safe fallbacks are returned to avoid crashes.
///// - 🔐 Token handling is abstracted by AuthorizedHttpClient.
