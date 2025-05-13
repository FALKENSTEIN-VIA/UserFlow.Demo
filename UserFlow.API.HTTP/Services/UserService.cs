/// @file UserService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Handles communication with the UserFlow API for managing users.
/// @details
/// Provides methods for retrieving, creating, updating, deleting, restoring, importing, and exporting users.
/// All methods use `AuthorizedHttpClient` to attach JWTs and interact with the backend API securely.

using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 👉 ✨ Service to access user-related endpoints in the UserFlow API.
/// </summary>
public class UserService : IUserService
{
    private readonly AuthorizedHttpClient _httpClient;

    /// <summary>
    /// 👉 ✨ Constructor with dependency injection.
    /// </summary>
    public UserService(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserDTO>> GetAllUsersAsync(bool includeCompany = false)
    {
        string url = includeCompany ? $"api/users?includeCompany=true" : $"api/users";
        var result = await _httpClient.GetAsync<List<UserDTO>>(url);
        return result ?? [];
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> GetUserByIdAsync(long id, bool includeCompany = false)
    {
        string url = includeCompany ? $"api/users/{id}?includeCompany=true" : $"api/users/{id}";
        var result = await _httpClient.GetAsync<UserDTO>(url);
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateUserAsync(UpdateUserDTO dto)
    {
        var response = await _httpClient.PutAsync("api/users", dto);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteUserAsync(long id)
    {
        var response = await _httpClient.DeleteAsync($"api/users/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<UserDTO?> RestoreUserAsync(long id)
    {
        var result = await _httpClient.PostAsync<object, UserDTO>($"api/users/{id}/restore", new { });
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> CreateUserByAdminAsync(CreateUserByAdminDTO dto)
    {
        var allowedRoles = new[] { "User", "Manager", "Admin" };

        if (!allowedRoles.Contains(dto.Role))
        {
            return false;
        }

        var response = await _httpClient.PostAsync("api/users/admin/create", dto);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<UserDTO>> BulkCreateUsersAsync(List<CreateUserByAdminDTO> dtos)
    {
        var allowedRoles = new[] { "User", "Manager", "Admin" };

        var invalidDtos = dtos
            .Select((dto, index) => new { dto, index })
            .Where(x => !allowedRoles.Contains(x.dto.Role))
            .ToList();

        if (invalidDtos.Any())
        {
            var errorList = invalidDtos.Select(x => new BulkOperationErrorDTO
            {
                RecordIndex = x.index,
                Field = "Role",
                Code = "InvalidRole",
                Message = $"Invalid role '{x.dto.Role}' for user '{x.dto.Email}'",
                Values = new Dictionary<string, object>
                {
                    { "Name", x.dto.Name },
                    { "Email", x.dto.Email },
                    { "Role", x.dto.Role }
                }
            }).ToList();

            return new BulkOperationResultDTO<UserDTO>
            {
                ImportedCount = 0,
                TotalRows = dtos.Count,
                Errors = errorList
            };
        }

        var response = await _httpClient.PostAsync<List<CreateUserByAdminDTO>, BulkOperationResultDTO<UserDTO>>("api/users/bulk", dtos);

        return response ?? new();
    }

    /// <inheritdoc/>
    public async Task<BulkOperationResultDTO<UserDTO>> ImportUsersAsync(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        content.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("api/users/import", content);
        var result = await response.Content.ReadFromJsonAsync<BulkOperationResultDTO<UserDTO>>() ?? new();

        return result;
    }

    /// <inheritdoc/>
    public async Task<byte[]> ExportUsersCsvAsync()
    {
        var response = await _httpClient.GetRawAsync("api/users/export");

        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();

        return bytes;
    }
}
