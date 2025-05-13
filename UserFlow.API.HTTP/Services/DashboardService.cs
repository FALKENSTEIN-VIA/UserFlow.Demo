/// *****************************************************************************************
/// @file DashboardService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides admin dashboard functionality for metrics, user imports, and exports.
/// @details
/// Implements IDashboardService using AuthorizedHttpClient for secure backend access.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Implementation of <see cref="IDashboardService"/> to access admin dashboard endpoints.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AuthorizedHttpClient _httpClient;

    /// <summary>
    /// 👉 ✨ Constructor injecting <see cref="AuthorizedHttpClient"/> and <see cref="ILogger"/>.
    /// </summary>
    public DashboardService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<int> GetUserCountAsync()
    {
        var result = await _httpClient.GetAsync<int>("api/dashboard/users/count");
        return result;
    }

    /// <inheritdoc/>
    public async Task<int> GetProjectCountAsync()
    {
        var result = await _httpClient.GetAsync<int>("api/dashboard/projects/count");
        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserDTO>?> GetLatestUsersAsync(int count = 5)
    {
        var result = await _httpClient.GetAsync<List<UserDTO>>($"api/dashboard/users/latest?count={count}");
        return result;
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<UserDTO>?> ImportUsersAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/dashboard/import/users", content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<UserDTO>>();
        return result;
    }

    /// <inheritdoc/>
    public async Task<Stream?> ExportUsersAsync()
    {
        var response = await _httpClient.GetRawAsync("api/dashboard/export/users");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadAsStreamAsync();
    }
}

/// <remarks>
/// Developer Notes:
/// - Uses AuthorizedHttpClient for token-authenticated access.
/// - Handles counts, latest users, and import/export operations.
/// - Logs errors and operation summaries with emojis for clarity.
/// - Returns 0 or null on failure to ensure UI stability.
/// *****************************************************************************************
