/// *****************************************************************************************
/// @file ProjectService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Implementation of project-related operations via the UserFlow API.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Implementation of <see cref="IProjectService"/> for communicating with the ProjectController endpoints.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly AuthorizedHttpClient _httpClient;

    /// <summary>
    /// 👉 ✨ Constructor injecting dependencies.
    /// </summary>
    public ProjectService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region 📥 CRUD Operations

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectDTO>> GetAllAsync()
    {
        var result = await _httpClient.GetAsync<List<ProjectDTO>>("api/projects") ?? [];
        return result;
    }

    /// <inheritdoc/>
    public async Task<ProjectDTO?> GetByIdAsync(long id)
    {
        var result = await _httpClient.GetAsync<ProjectDTO>($"api/projects/{id}");
        return result;
    }

    /// <inheritdoc/>
    public async Task<ProjectDTO> CreateAsync(ProjectCreateDTO dto)
    {
        var result = await _httpClient.PostAsync<ProjectCreateDTO, ProjectDTO>("api/projects", dto);
        if (result == null)
        {
            throw new InvalidOperationException("CreateProjectAsync failed");
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(long id, ProjectUpdateDTO dto)
    {
        var response = await _httpClient.PutAsync($"api/projects/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/projects/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<ProjectDTO?> RestoreAsync(long id)
    {
        var result = await _httpClient.PostAsync<object, ProjectDTO>($"api/projects/{id}/restore", new { });
        return result;
    }

    #endregion

    #region 📦 Bulk Operations

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<ProjectDTO>> BulkCreateAsync(List<ProjectCreateDTO> list)
    {
        var result = await _httpClient.PostAsync<List<ProjectCreateDTO>, BulkOperationResultDTO<ProjectDTO>>("api/projects/bulk", list)
                     ?? new();
        return result;
    }

    #endregion

    #region 📃 Pagination

    /// <inheritdoc/>
    public async Task<PagedResultDTO<ProjectDTO>> GetPagedAsync(int page, int pageSize)
    {
        var result = await _httpClient.GetAsync<PagedResultDTO<ProjectDTO>>($"api/projects/paged?page={page}&pageSize={pageSize}")
                     ?? new() { Page = page, PageSize = pageSize, Items = [] };

        return result;
    }

    #endregion

    #region 📁 Import & Export

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<ProjectDTO>> ImportAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/projects/import", content);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Import failed");
        }

        var result = await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<ProjectDTO>>() ?? new();
        return result;
    }

    /// <inheritdoc/>
    public async Task<Stream> ExportAsync()
    {
        var response = await _httpClient.GetRawAsync("api/projects/export");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Export failed");
        }

        return await response.Content.ReadAsStreamAsync();
    }

    #endregion
}

/// <remarks>
/// Developer Notes:
/// - 🧱 Handles all CRUD, restore, bulk, and CSV operations for `ProjectController`.
/// - 📄 Exports and imports support `IFormFile` uploads and streaming downloads.
/// - 💥 Error handling is consistent and informative, with clear logging output.
/// - 🧼 Use dependency injection to register this service with `IProjectService`.
/// - 🔐 Assumes authorization headers are handled via global message handler or pipeline.
