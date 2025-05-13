/// *****************************************************************************************
/// @file SecureTokenStore.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-10
/// @brief Secure token storage implementation for MAUI using SecureStorage.
/// @details
/// Implements the ISecureTokenStore interface and provides secure access to JWT access and refresh tokens.
/// Only non-empty values are stored to avoid exceptions on WinUI.
/// *****************************************************************************************

using UserFlow.API.HTTP.Services.Interfaces;

namespace UserFlow.Maui.Client.Services;

/// <summary>
/// 🔐 Secure token storage service for MAUI using SecureStorage.
/// </summary>
public class SecureTokenStore : ISecureTokenStore
{
    /// <summary>
    /// 💾 Stores both access and refresh tokens in secure storage.
    /// Only non-empty values will be stored to avoid platform exceptions.
    /// </summary>
    public async Task SaveTokensAsync(string token, string refreshToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(token))
                await SecureStorage.SetAsync("access_token", token);

            if (!string.IsNullOrEmpty(refreshToken))
                await SecureStorage.SetAsync("refresh_token", refreshToken);
        }
        catch (Exception ex)
        {
            // ⚠️ SecureStorage can fail on WinUI if value is null/empty
            Console.WriteLine($"❌ Token storage failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 🔑 Retrieves the stored access token (used for API calls).
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync("access_token");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to load access token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 🔁 Retrieves the stored refresh token (used for silent renewal).
    /// </summary>
    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync("refresh_token");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to load refresh token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 🧹 Clears all stored tokens from secure storage.
    /// </summary>
    public async Task ClearTokensAsync()
    {
        try
        {
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to clear tokens: {ex.Message}");
        }
    }
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - ✅ Uses `SecureStorage` for platform-independent secure token handling.
/// - ⚠️ Avoids saving empty strings to prevent exceptions (esp. on WinUI).
/// - 🔐 Implements `ISecureTokenStore` interface for DI-based access.
/// - 📦 Used by `AuthService` and `AuthorizedHttpClient` for token handling.
/// - 🧪 Always await `SaveTokensAsync` to ensure proper persistence.
/// </remarks>
