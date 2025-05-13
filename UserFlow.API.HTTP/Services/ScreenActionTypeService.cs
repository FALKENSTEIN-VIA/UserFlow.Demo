/// @file ScreenActionTypeService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides API communication for managing ScreenActionTypes via AuthorizedHttpClient.

using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Implementation of IScreenActionTypeService to access screen action type endpoints.
/// </summary>
public class ScreenActionTypeService : IScreenActionTypeService
{
    private readonly AuthorizedHttpClient _httpClient;

    /// <summary>
    /// 👉 ✨ Constructor to inject HttpClient and Logger.
    /// </summary>
    public ScreenActionTypeService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region 📄 CRUD

    /// <inheritdoc/>
    public async Task<IEnumerable<ScreenActionTypeDTO>> GetAllAsync()
    {
        return await _httpClient.GetAsync<List<ScreenActionTypeDTO>>("api/action-types") ?? [];
    }

    /// <inheritdoc/>
    public async Task<ScreenActionTypeDTO> CreateAsync(ScreenActionTypeCreateDTO dto)
    {
        var result = await _httpClient.PostAsync<ScreenActionTypeCreateDTO, ScreenActionTypeDTO>("api/action-types", dto);
        if (result == null)
        {
            throw new InvalidOperationException("Empty response from CreateAsync");
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(long id, ScreenActionTypeUpdateDTO dto)
    {
        var response = await _httpClient.PutAsync($"api/action-types/{id}", dto);
        return ParseSuccess(response, $"UpdateAsync ID={id}");
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/action-types/{id}");
        return ParseSuccess(response, $"DeleteAsync ID={id}");
    }

    /// <inheritdoc/>
    public async Task<ScreenActionTypeDTO?> RestoreAsync(long id)
    {
        var result = await _httpClient.PostAsync<object, ScreenActionTypeDTO>($"api/action-types/{id}/restore", new { });
        return result;
    }

    #endregion

    #region 📦 Bulk & Paged Operations

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<ScreenActionTypeDTO>> BulkCreateAsync(List<ScreenActionTypeCreateDTO> list)
    {
        var result = await _httpClient.PostAsync<List<ScreenActionTypeCreateDTO>, BulkOperationResultDTO<ScreenActionTypeDTO>>(
            "api/action-types/bulk", list
        );
        return result ?? new BulkOperationResultDTO<ScreenActionTypeDTO>();
    }

    /// <inheritdoc/>
    public async Task<PagedResultDTO<ScreenActionTypeDTO>> GetPagedAsync(int page, int pageSize)
    {
        var result = await _httpClient.GetAsync<PagedResultDTO<ScreenActionTypeDTO>>(
            $"api/action-types/paged?page={page}&pageSize={pageSize}"
        );

        return result ?? new PagedResultDTO<ScreenActionTypeDTO> { Page = page, PageSize = pageSize, Items = [] };
    }

    #endregion

    #region 📥 Import / 📤 Export

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<ScreenActionTypeDTO>> ImportAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/action-types/import", content);
        var result = await ParseResponse<BulkOperationResultDTO<ScreenActionTypeDTO>>(response, "ImportAsync");
        return result ?? new BulkOperationResultDTO<ScreenActionTypeDTO>();
    }

    /// <inheritdoc/>
    public async Task<Stream> ExportAsync()
    {
        var response = await _httpClient.GetRawAsync("api/action-types/export");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Export failed");
        }

        return await response.Content.ReadAsStreamAsync();
    }

    #endregion

    #region 🧠 Helpers

    private async Task<T?> ParseResponse<T>(HttpResponseMessage response, string context)
    {
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private bool ParseSuccess(HttpResponseMessage response, string context) => response.IsSuccessStatusCode;

    #endregion
}

/// <remarks>
/// Developer Notes:
/// - 🧱 Manages full lifecycle of ScreenActionTypes: CRUD, restore, pagination, bulk, import/export.
/// - 🔐 Uses `AuthorizedHttpClient` to ensure token-based authentication with automatic refresh.
/// - 📁 CSV file operations use `MultipartFormDataContent` and `IFormFile`.
/// - 🧠 Logs all failures with context information for diagnostics.
/// - 📦 Returns typed DTOs for seamless ViewModel binding and UI feedback.
