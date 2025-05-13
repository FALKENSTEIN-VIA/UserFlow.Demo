/// *****************************************************************************************
/// @file ITokenRefreshService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-08
/// @brief Interface for refresh token functionality to break DI recursion.
/// @details
/// Provides a minimal interface used by AuthorizedHttpClient to refresh expired access tokens.
/// This abstraction prevents circular dependency between AuthService and AuthorizedHttpClient.
/// *****************************************************************************************

namespace UserFlow.API.Http.Auth;

/// <summary>
/// 🔄 Interface for refresh token functionality used by AuthorizedHttpClient.
/// </summary>
public interface ITokenRefreshService
{
    /// <summary>
    /// 🔁 Attempts to refresh the JWT token using a stored refresh token.
    /// </summary>
    /// <returns>Returns the new access token if successful, or null if refresh failed.</returns>
    Task<string?> TryRefreshTokenAsync();
}

/// <remarks>
/// 🧩 **Developer Notes**
/// - Implemented by `AuthService`, which handles communication with the `/auth/refresh` endpoint.
/// - Designed specifically to **decouple** the `AuthorizedHttpClient` from full `IAuthService`.
/// - 🔐 This interface is minimal to avoid circular dependency when injecting both classes via DI.
/// - 📦 Always returns the *access token* as string — never full `AuthResponseDTO`.
/// </remarks>
