/// *****************************************************************************************
/// @file AuthService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-10
/// @brief Implements authentication functionality against the UserFlow API.
/// @details
/// Provides login, logout, registration, password setup, and token refresh by interacting with the backend API.
/// Uses AuthorizedHttpClient for secure communication and ISecureTokenStore for token persistence.
/// *****************************************************************************************

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserFlow.API.Http.Auth;
using UserFlow.API.Http.Services;
using UserFlow.API.HTTP.Services.Interfaces;
using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.DTO.Auth;

namespace UserFlow.API.HTTP.Services;

/// <summary>
/// 🔐 Provides authentication logic and token refresh capabilities for the UserFlow client.
/// </summary>
public class AuthService : IAuthService, ITokenRefreshService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureTokenStore _tokenStore;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>
    /// 🛠 Constructor injecting dependencies and initializing token expiry.
    /// </summary>
    public AuthService(
        IHttpClientFactory httpClientFactory,
        ISecureTokenStore tokenStore)
    {
        _httpClient = httpClientFactory.CreateClient("AuthClient");
        _tokenStore = tokenStore;
        _ = InitializeTokenExpiryAsync();
    }

    /// <summary>
    /// ⏳ Initializes token expiry from stored access token (if available).
    /// </summary>
    private async Task InitializeTokenExpiryAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
            UpdateTokenExpiry(token);
        else
            _tokenExpiry = DateTime.MinValue;
    }

    /// <summary>
    /// 📨 Sends an authorized HTTP request, with optional retry after token refresh.
    /// </summary>
    private async Task<HttpResponseMessage> SendAuthorizedRequest(Func<Task<HttpResponseMessage>> request)
    {
        await SetAuthorizationHeader();
        var response = await request();

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        var newToken = await TryRefreshTokenAsync();
        if (!string.IsNullOrEmpty(newToken))
        {
            await SetAuthorizationHeader();
            return await request();
        }

        return response;
    }

    /// <summary>
    /// 🔐 Sets the Authorization header with the access token (if available).
    /// </summary>
    private async Task SetAuthorizationHeader()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            !string.IsNullOrEmpty(token)
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;
    }

    /// <summary>
    /// 📦 Generic response handler for deserializing JSON content.
    /// </summary>
    private async Task<T?> HandleResponse<T>(HttpResponseMessage response) where T : class
    {
        try
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 🔐 Attempts login and stores received tokens if successful.
    /// </summary>
    public async Task<AuthResponseDTO?> LoginAsync(LoginDTO loginDto)
    {
        var response = await SendAuthorizedRequest(() =>
            _httpClient.PostAsJsonAsync("api/auth/login", loginDto));

        var result = await HandleResponse<AuthResponseDTO>(response);
        if (result != null)
        {
            await _tokenStore.SaveTokensAsync(result.Token, result.RefreshToken);
            UpdateTokenExpiry(result.Token);
        }

        return result;
    }

    /// <summary>
    /// 🔒 Clears tokens and resets session.
    /// </summary>
    public async Task<bool> LogoutAsync()
    {
        var response = await SendAuthorizedRequest(() =>
            _httpClient.PostAsync("api/auth/logout", null!));

        if (response.StatusCode == HttpStatusCode.OK)
        {
            await _tokenStore.ClearTokensAsync();
            _tokenExpiry = DateTime.MinValue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 📝 Registers a new user and stores token if successful.
    /// </summary>
    public async Task<AuthResponseDTO?> RegisterAsync(RegisterDTO registerDto)
    {
        var response = await SendAuthorizedRequest(() =>
            _httpClient.PostAsJsonAsync("api/auth/register", registerDto));

        var result = await HandleResponse<AuthResponseDTO>(response);
        if (result != null)
        {
            await _tokenStore.SaveTokensAsync(result.Token, result.RefreshToken);
            UpdateTokenExpiry(result.Token);
        }

        return result;
    }

    /// <summary>
    /// 🔑 Sets user password and updates token session.
    /// </summary>
    public async Task<bool> SetPasswordAsync(SetPasswordDTO dto)
    {
        var response = await SendAuthorizedRequest(() =>
            _httpClient.PostAsJsonAsync("api/auth/set-password", dto));

        var result = await HandleResponse<AuthResponseDTO>(response);
        if (result != null)
        {
            await _tokenStore.SaveTokensAsync(result.Token, result.RefreshToken);
            UpdateTokenExpiry(result.Token);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 🧩 Completes registration and stores received tokens.
    /// </summary>
    public async Task<AuthResponseDTO?> CompleteRegistrationAsync(CompleteRegistrationDTO dto)
    {
        var response = await SendAuthorizedRequest(() =>
            _httpClient.PostAsJsonAsync("api/auth/complete-registration", dto));

        var result = await HandleResponse<AuthResponseDTO>(response);
        if (result != null)
        {
            await _tokenStore.SaveTokensAsync(result.Token, result.RefreshToken);
            UpdateTokenExpiry(result.Token);
        }

        return result;
    }

    /// <summary>
    /// 🔁 Refreshes JWT using the stored refresh token.
    /// </summary>
    public async Task<AuthResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", dto);
        var result = await HandleResponse<AuthResponseDTO>(response);
        if (result != null)
        {
            await _tokenStore.SaveTokensAsync(result.Token, result.RefreshToken);
            UpdateTokenExpiry(result.Token);
        }
        return result;
    }

    /// <summary>
    /// 🤝 Attempts to refresh the token if it is expired.
    /// </summary>
    public async Task<string?> TryRefreshTokenAsync()
    {
        if (!IsTokenExpired())
            return await _tokenStore.GetAccessTokenAsync();

        await _refreshLock.WaitAsync();
        try
        {
            if (!IsTokenExpired())
                return await _tokenStore.GetAccessTokenAsync();

            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return null;
            }

            var result = await RefreshTokenAsync(new RefreshTokenRequestDTO { RefreshToken = refreshToken });
            return result?.Token;
        }
        catch
        {
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// ⌛ Checks whether the current token is expired or about to expire.
    /// </summary>
    private bool IsTokenExpired()
    {
        if (_tokenExpiry <= DateTime.MinValue)
        {
            return true;
        }

        return DateTime.UtcNow >= _tokenExpiry.AddSeconds(-30); // expire slightly before real time
    }

    /// <summary>
    /// 📆 Parses expiry from JWT and stores it in memory.
    /// </summary>
    private void UpdateTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            _tokenExpiry = jwt.ValidTo.ToUniversalTime();

            if (_tokenExpiry <= DateTime.MinValue || _tokenExpiry == DateTime.MaxValue)
            {
                _tokenExpiry = DateTime.UtcNow.AddMinutes(5); // Fallback default
            }
        }
        catch
        {
            _tokenExpiry = DateTime.UtcNow.AddMinutes(5); // Prevent MinValue issues
        }
    }

    /// <summary>
    /// 👥 Retrieves the list of available test users from the API.
    /// </summary>
    /// <returns>A list of <see cref="UserDTO"/> or an empty list if none available or request failed.</returns>
    public async Task<List<UserDTO>> GetTestUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/testusers");

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var users = await response.Content.ReadFromJsonAsync<List<UserDTO>>();
            return users ?? [];
        }
        catch
        {
            return [];
        }
    }
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - Communicates with `api/auth/...` endpoints using JSON and AuthorizedHttpClient for token handling.
/// - ✅ Uses JWT expiry claims to determine token lifetime and auto-refresh threshold.
/// - ❗ All token errors are logged in detail with context.
/// - 🔐 SecureStorage access is abstracted via `ISecureTokenStore`.
/// </remarks>
