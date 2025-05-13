/// *****************************************************************************************
/// @file RefreshTokenRequestDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO for requesting a new JWT token using a valid refresh token.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 🔁 DTO used to request a new access token using an existing refresh token.
/// </summary>
/// <remarks>
/// This DTO is sent by the client when the original JWT access token has expired.
/// It contains both the expired token and a valid refresh token, which are verified
/// by the API to issue a new access token if the refresh token is valid and unexpired.
/// </remarks>
public class RefreshTokenRequestDTO
{
    /// <summary>
    /// 🪪 The expired or soon-to-expire JWT access token.
    /// </summary>
    /// <value>
    /// The original JWT token issued during login.
    /// </value>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 🔁 A valid refresh token associated with the user.
    /// </summary>
    /// <value>
    /// Must match a stored refresh token in the database.
    /// </value>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 👤 The ID of the user requesting the token refresh.
    /// </summary>
    /// <value>
    /// Used to locate the correct refresh token record.
    /// </value>
    public long UserId { get; set; }

    /// <summary>
    /// 🕓 The UTC timestamp when the refresh token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// ⏳ The UTC timestamp when the refresh token will expire.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// @remarks
/// 🔐 **Security Notes**
/// - Refresh tokens must be stored securely in the database and expire after a limited time.
/// - This DTO is used with `POST /api/auth/refresh`.
/// - Ensure tokens are compared using a constant-time comparison to avoid timing attacks.
///
/// ✅ **Validation**
/// - The server validates the refresh token, user ID, and expiration time.
/// - If the token is invalid, the request is rejected and the user must reauthenticate.
///
/// 📦 **Usage**
/// - This DTO is consumed by the API to return a new `AuthResponseDTO` with fresh tokens.
/// - Refresh tokens are typically stored in HttpOnly cookies or secure client storage.
/// *****************************************************************************************
