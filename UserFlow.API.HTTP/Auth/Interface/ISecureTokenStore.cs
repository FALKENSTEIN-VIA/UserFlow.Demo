/// @file ISecureTokenStore.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Interface for secure access/refresh token storage (platform-dependent implementation)
/// *****************************************************************************************

namespace UserFlow.API.HTTP.Services.Interfaces;

/// <summary>
/// 🔐 Defines access to token storage (SecureStorage or similar).
/// </summary>
public interface ISecureTokenStore
{
    Task SaveTokensAsync(string token, string refreshToken);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task ClearTokensAsync();
}
