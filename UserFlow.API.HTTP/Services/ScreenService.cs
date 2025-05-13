/// @file ScreenService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides API access for screen-related operations such as CRUD, bulk handling, paging, import/export.

using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Implements IScreenService to access screen-related API endpoints.
/// </summary>
public class ScreenService : IScreenService
{
    private readonly AuthorizedHttpClient _httpClient;

    /// <summary>
    /// 👉 ✨ Constructor to inject AuthorizedHttpClient and Logger.
    /// </summary>
    public ScreenService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region 📄 CRUD Operations

    /// <inheritdoc/>
    public async Task<IEnumerable<ScreenDTO>> GetAllAsync()
    {
        return await _httpClient.GetAsync<List<ScreenDTO>>("api/screens") ?? [];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ScreenDTO>> GetByProjectIdAsync(long projectId)
    {
        return await _httpClient.GetAsync<List<ScreenDTO>>($"api/screens/by-project/{projectId}") ?? [];
    }

    /// <inheritdoc/>
    public async Task<ValidationResponseDTO> CreateAsync(ScreenCreateDTO dto)
    {
        var result = await _httpClient.PostAsync<ScreenCreateDTO, ValidationResponseDTO>("api/screens", dto);
        return result ?? new ValidationResponseDTO { Success = false, Message = "Screen creation failed." };
    }

    /// <inheritdoc/>
    public async Task<ValidationResponseDTO> UpdateAsync(long id, ScreenUpdateDTO dto)
    {
        var result = await _httpClient.PostAsync<ScreenUpdateDTO, ValidationResponseDTO>($"api/screens/{id}", dto);
        return result ?? new ValidationResponseDTO { Success = false, Message = "Screen update failed." };
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/screens/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<ScreenDTO?> RestoreAsync(long id)
    {
        var result = await _httpClient.PostAsync<object, ScreenDTO>($"api/screens/{id}/restore", new { });
        return result;
    }

    #endregion

    #region 📦 Bulk & Paged

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<ScreenDTO>> BulkCreateAsync(List<ScreenCreateDTO> list)
    {
        var result = await _httpClient.PostAsync<List<ScreenCreateDTO>, BulkOperationResultDTO<ScreenDTO>>("api/screens/bulk", list);
        return result ?? new();
    }

    /// <inheritdoc/>
    public async Task<PagedResultDTO<ScreenDTO>> GetPagedAsync(int page, int pageSize)
    {
        var result = await _httpClient.GetAsync<PagedResultDTO<ScreenDTO>>($"api/screens/paged?page={page}&pageSize={pageSize}");
        return result ?? new PagedResultDTO<ScreenDTO> { Page = page, PageSize = pageSize, Items = [] };
    }

    #endregion

    #region 📥 Import / 📤 Export

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<ScreenDTO>> ImportAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/screens/import", content);
        var result = await ParseResponse<BulkOperationResultDTO<ScreenDTO>>(response, "ImportAsync");
        return result ?? new();
    }

    /// <inheritdoc/>
    public async Task<Stream> ExportAsync()
    {
        var response = await _httpClient.GetRawAsync("api/screens/export");

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
/// - 📺 Provides full API access to `/api/screens` including filtering by project.
/// - 💬 Supports `ValidationResponseDTO` for create/update operations (UI-friendly).
/// - 📂 CSV import/export supported via `MultipartFormDataContent`.
/// - 🔐 Uses `AuthorizedHttpClient` to manage JWT authentication with automatic refresh.
/// - 🔄 Consistent logging using Serilog-compatible `ILogger`.
/// - 📦 Bulk and paged operations return structured results for efficient UI data binding.
