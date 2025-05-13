/// *****************************************************************************************
/// @file CompanyService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-13
/// @brief Implementation of ICompanyService using AuthorizedHttpClient to access the UserFlow API.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 📡 Communicates with the UserFlow API to manage companies.
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly AuthorizedHttpClient _httpClient;

    public CompanyService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 🚀 Gets all companies (paged with defaults).
    /// </summary>
    public async Task<IEnumerable<CompanyDTO>> GetAllAsync()
    {
        var pagedResult = await _httpClient.GetAsync<PagedResultDTO<CompanyDTO>>("api/companies");
        return pagedResult?.Items ?? [];
    }

    /// <summary>
    /// 🔍 Gets a company by its ID.
    /// </summary>
    public async Task<CompanyDTO?> GetByIdAsync(long id)
    {
        return await _httpClient.GetAsync<CompanyDTO>($"api/companies/{id}");
    }

    /// <summary>
    /// ➕ Creates a new company.
    /// </summary>
    public async Task<CompanyDTO> CreateCompanyAsync(CompanyCreateDTO dto)
    {
        var result = await _httpClient.PostAsync<CompanyCreateDTO, CompanyDTO>("api/companies", dto);
        if (result == null)
            throw new InvalidOperationException("Company response was empty.");
        return result;
    }

    /// <summary>
    /// ✏️ Updates an existing company.
    /// </summary>
    public async Task<bool> UpdateCompanyAsync(long id, CompanyUpdateDTO dto)
    {
        var response = await _httpClient.PutAsync($"api/companies", dto);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// ❌ Soft-deletes a company.
    /// </summary>
    public async Task<bool> DeleteCompanyAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/companies/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// ♻️ Restores a soft-deleted company.
    /// </summary>
    public async Task<CompanyDTO?> RestoreCompanyAsync(long id)
    {
        var result = await _httpClient.PostAsync<object, CompanyDTO>($"api/companies/{id}/restore", new { });
        return result;
    }

    /// <summary>
    /// 📥 Bulk creates companies.
    /// </summary>
    public async Task<BulkOperationResultDTO<CompanyDTO>> BulkCreateCompaniesAsync(List<CompanyCreateDTO> companies)
    {
        var result = await _httpClient.PostAsync<List<CompanyCreateDTO>, BulkOperationResultDTO<CompanyDTO>>("api/companies/bulk", companies)
                     ?? new();
        return result;
    }

    /// <summary>
    /// 📄 Gets paged companies.
    /// </summary>
    public async Task<PagedResultDTO<CompanyDTO>> GetPagedCompaniesAsync(int page, int pageSize)
    {
        var result = await _httpClient.GetAsync<PagedResultDTO<CompanyDTO>>($"api/companies?page={page}&pageSize={pageSize}")
                     ?? new() { Items = [], Page = page, PageSize = pageSize };
        return result;
    }

    /// <summary>
    /// 📥 Imports companies from CSV.
    /// </summary>
    public async Task<BulkOperationResultDTO<CompanyDTO>> ImportCompaniesAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/companies/import", content);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Import failed");

        var result = await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<CompanyDTO>>() ?? new();
        return result;
    }

    /// <summary>
    /// 📤 Exports companies as CSV.
    /// </summary>
    public async Task<Stream> ExportCompaniesAsync()
    {
        var response = await _httpClient.GetRawAsync("api/companies/export");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Export failed");

        return await response.Content.ReadAsStreamAsync();
    }

    /// <summary>
    /// 🏢 Registers a new company and returns AuthResponseDTO.
    /// </summary>
    public async Task<AuthResponseDTO?> RegisterCompanyAsync(CompanyRegisterDTO dto)
    {
        return await _httpClient.PostAsync<CompanyRegisterDTO, AuthResponseDTO>("api/companies/register", dto);
    }
}

/// <remarks>
/// Developer Notes:
/// - All URLs normalized to **api/companies/…** (plural, consistent with Controller)
/// - Always expects **PagedResultDTO** for paged endpoints
/// - ❗ Deserializing lists from paged API now works as expected, no mismatch
/// - Uses **HttpResponseMessage.IsSuccessStatusCode** where applicable
/// </remarks>
