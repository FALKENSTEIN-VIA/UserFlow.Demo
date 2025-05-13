/// @file IAuthService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Interface for authentication services including JWT and refresh token handling.
/// @details
/// Defines authentication operations such as login, JWT issuance, and refresh token validation. 
/// Used throughout the application to manage user authentication workflows in a consistent and secure manner.

namespace UserFlow.API.Services;

/// <summary>
/// 👉 ✨ Defines authentication service operations like login, token generation, and refresh token handling.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 👉 ✨ Authenticates a user with email and password.
    /// </summary>
    /// <param name="email">📧 User email address.</param>
    /// <param name="password">🔐 User password.</param>
    /// <returns>Returns a JWT token string if authentication succeeds; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// - ✅ Called during user login to verify credentials.
    /// - 🛡️ Returns a valid JWT token if email/password are correct.
    /// - ❌ Returns <c>null</c> on authentication failure.
    /// </remarks>
    Task<string?> LoginAsync(string email, string password);

    /// <summary>
    /// 👉 ✨ Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="token">🧾 Expired JWT access token.</param>
    /// <param name="refreshToken">♻️ Refresh token used for renewal.</param>
    /// <returns>Returns a new JWT token string if successful; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// - 🔁 Validates the refresh token and issues a new JWT token.
    /// - ❌ Returns <c>null</c> if the refresh token is invalid or expired.
    /// - ⚠️ Must be called before the refresh token itself expires.
    /// </remarks>
    //Task<string?> RefreshTokenAsync(string token, string refreshToken);
}

/// @remarks
/// Developer Notes:
/// - 📦 This interface abstracts the authentication logic for login and token renewal.
/// - 🚀 LoginAsync handles credential verification and returns a signed JWT token.
/// - 🔄 RefreshTokenAsync allows token renewal without forcing the user to log in again.
/// - ⚡ Both methods are asynchronous to support scalable web request handling.
/// - 🔐 Use in conjunction with JwtService and Identity to implement secure auth flows.
