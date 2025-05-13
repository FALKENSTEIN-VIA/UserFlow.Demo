/// *****************************************************************************************
/// @file NoteService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides implementation for accessing and managing notes via the UserFlow API.
/// *****************************************************************************************

using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Implementation of <see cref="INoteService"/> to access the Notes API.
/// </summary>
public class NoteService : INoteService
{
    private readonly AuthorizedHttpClient _httpClient;

    /// <summary>
    /// 👉 ✨ Constructor injecting dependencies.
    /// </summary>
    public NoteService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region 📥 CRUD Operations

    /// <inheritdoc/>
    public async Task<IEnumerable<NoteDTO>?> GetAllAsync()
    {
        var result = await _httpClient.GetAsync<List<NoteDTO>>("api/notes");
        return result;
    }

    /// <inheritdoc/>
    public async Task<NoteDTO?> GetByIdAsync(long id)
    {
        var result = await _httpClient.GetAsync<NoteDTO>($"api/notes/{id}");
        return result;
    }


    /// <inheritdoc/>
    public async Task<NoteDTO?> CreateNoteAsync(NoteDTO dto)
    {
        var result = await _httpClient.PostAsync<NoteDTO, NoteDTO>("api/notes", dto);
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateNoteAsync(long id, NoteDTO dto)
    {
        var response = await _httpClient.PutAsync($"api/notes/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteNoteAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/notes/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<NoteDTO?> RestoreNoteAsync(long id)
    {
        var result = await _httpClient.PostAsync<object, NoteDTO>($"api/notes/{id}/restore", new { });
        return result;
    }

    #endregion

    #region 📁 Import & Export

    /// <inheritdoc/>
    public async Task<Stream?> ExportNotesAsync()
    {
        var response = await _httpClient.GetRawAsync("api/notes/export");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStreamAsync();
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<NoteDTO>?> ImportNotesAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/notes/import", content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<NoteDTO>>();
        return result;
    }

    #endregion
}

/// <remarks>
/// Developer Notes:
/// - ✅ Uses AuthorizedHttpClient for secure and token-aware requests.
/// - 📝 Full support for CRUD, restore, bulk import/export of notes.
/// - 🧠 All failures are logged, null-safe fallbacks are returned.
/// - 📤 Import/export relies on `MultipartFormDataContent` and stream download.
/// - 🔐 Token and authentication are managed globally in AuthorizedHttpClient.
