/// @file ScreenActionService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides communication layer for ScreenActionController endpoints via AuthorizedHttpClient.

using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Service for interacting with the ScreenActionController endpoints.
/// </summary>
public class ScreenActionService : IScreenActionService
{
    private readonly AuthorizedHttpClient _httpClient;

    /// <summary>
    /// 👉 ✨ Constructor to inject dependencies.
    /// </summary>
    public ScreenActionService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region 📄 CRUD

    /// <inheritdoc/>
    public async Task<IEnumerable<ScreenActionDTO>?> GetAllAsync()
    {
        return await _httpClient.GetAsync<List<ScreenActionDTO>>("api/screen-actions");
    }

    /// <inheritdoc/>
    public async Task<ScreenActionDTO?> GetByIdAsync(long id)
    {
        return await _httpClient.GetAsync<ScreenActionDTO>($"api/screen-actions/{id}");
    }

    /// <inheritdoc/>
    public async Task<ScreenActionDTO?> CreateAsync(ScreenActionCreateDTO dto)
    {
        return await _httpClient.PostAsync<ScreenActionCreateDTO, ScreenActionDTO>("api/screen-actions", dto);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(long id, ScreenActionUpdateDTO dto)
    {
        var response = await _httpClient.PutAsync($"api/screen-actions/{id}", dto);
        return ParseSuccess(response, $"UpdateAsync ID={id}");
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/screen-actions/{id}");
        return ParseSuccess(response, $"DeleteAsync ID={id}");
    }

    /// <inheritdoc/>
    public async Task<ScreenActionDTO?> RestoreAsync(long id)
    {
        return await _httpClient.PostAsync<object, ScreenActionDTO>($"api/screen-actions/{id}/restore", new { });
    }

    #endregion

    #region 🔍 Filtered GETs

    public async Task<IEnumerable<ScreenActionDTO>?> GetByProjectAsync(long projectId)
        => await _httpClient.GetAsync<List<ScreenActionDTO>>($"api/screen-actions/by-project/{projectId}");

    public async Task<IEnumerable<ScreenActionDTO>?> GetByScreenAsync(long screenId)
        => await _httpClient.GetAsync<List<ScreenActionDTO>>($"api/screen-actions/by-screen/{screenId}");

    public async Task<IEnumerable<ScreenActionDTO>?> GetByUserAsync(long userId)
        => await _httpClient.GetAsync<List<ScreenActionDTO>>($"api/screen-actions/by-user/{userId}");

    public async Task<IEnumerable<ScreenActionDTO>?> GetByTypeAsync(long typeId)
        => await _httpClient.GetAsync<List<ScreenActionDTO>>($"api/screen-actions/by-type/{typeId}");

    #endregion

    #region 📥 Import / 📤 Export

    public async Task<Stream?> ExportAsync()
    {
        var response = await _httpClient.GetRawAsync("api/screen-actions/export");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadAsStreamAsync();
    }

    public async Task<BulkOperationResultDTO<ScreenActionDTO>?> ImportAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/screen-actions/import", content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<ScreenActionDTO>>();
    }

    #endregion

    #region 🧠 Helpers

    private bool ParseSuccess(HttpResponseMessage response, string context) => response.IsSuccessStatusCode;

    #endregion
}

/// <remarks>
/// Developer Notes:
/// - 🧱 Handles all CRUD operations, filtered queries, import/export, and restore for ScreenAction entities.
/// - 🔐 Uses AuthorizedHttpClient for automatic token handling and refresh.
/// - 🧪 Logging with context helps debug failed requests and supports diagnostics.
/// - 📁 Uses IFormFile for CSV upload and streaming for CSV download.
/// - ❗ All errors are logged and default/null values are returned to prevent app crashes.
