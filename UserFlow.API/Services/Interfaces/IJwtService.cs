/// @file IJwtService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Interface for handling JWT token creation for users.
/// @details
/// Defines methods to create access tokens and refresh tokens based on user identity.
/// Used after login or registration to issue authentication tokens for API communication.

using UserFlow.API.Data.Entities;

namespace UserFlow.API.Services;

/// <summary>
/// 👉 ✨ Provides functionality for generating JWT access and refresh tokens.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 👉 ✨ Creates a JWT access token for a given user.
    /// </summary>
    /// <param name="user">👤 The user entity for which the token will be created.</param>
    /// <returns>Returns a signed JWT token as a <see cref="string"/>.</returns>
    /// <remarks>
    /// - ✅ Called immediately after successful login or registration.
    /// - 🔐 Token includes claims like UserId and Role.
    /// - 🧾 Used for authenticating subsequent API requests.
    /// </remarks>
    Task<string> CreateToken(User user);

    /// <summary>
    /// 👉 ✨ Creates a refresh token for the specified user ID.
    /// </summary>
    /// <param name="userId">🔑 ID of the user who needs a refresh token.</param>
    /// <returns>A secure, random refresh token string.</returns>
    /// <remarks>
    /// - 🔁 Used to obtain a new access token without re-authentication.
    /// - ♻️ Should be stored in the database and validated during token refresh.
    /// </remarks>
    Task<string> CreateRefreshTokenAsync(long userId);
}

/// @remarks
/// Developer Notes:
/// - 📦 This interface is responsible for JWT generation logic including user claims and token signing.
/// - 🧠 `CreateToken` generates a short-lived access token with user context for API access.
/// - 🔄 `CreateRefreshTokenAsync` provides a long-lived token for renewing access tokens without login.
/// - ✨ Use alongside Identity and AuthService to implement secure, stateless authentication.
/// - 🔐 Tokens should be signed using a secure key and validated on every request.
