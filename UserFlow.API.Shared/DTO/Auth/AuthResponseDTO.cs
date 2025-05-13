/// *****************************************************************************************
/// @file AuthResponseDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO for authentication response including JWT and refresh token.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 🔐✨ Authentication response returned after successful login or registration.
/// </summary>
/// <remarks>
/// This DTO contains the signed JWT token, a refresh token for re-authentication,
/// and the user profile data. It is sent to the client after successful login
/// and can be stored locally (e.g., in secure storage).
/// </remarks>
public class AuthResponseDTO
{
    /// <summary>
    /// 🔑 The primary JWT token used for API authentication.
    /// </summary>
    /// <value>
    /// A signed JSON Web Token (JWT) string that should be included in the `Authorization` header of API requests.
    /// </value>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ♻️ A refresh token used to obtain a new JWT when the current one expires.
    /// </summary>
    /// <value>
    /// A long-lived token that can be exchanged for a new access token without requiring re-login.
    /// </value>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 👤 User profile returned after authentication.
    /// </summary>
    /// <value>
    /// A DTO representing the currently authenticated user.
    /// Includes user ID, email, name, roles, and possibly company reference.
    /// </value>
    public UserDTO? User { get; set; }
}

/// @remarks
/// 🛠️ **Developer Notes**
/// 
/// - This class is returned by `/api/auth/login`, `/api/auth/register`, and `/api/auth/complete-registration`.
/// - Contains all authentication-related data required for session handling on the client.
/// - Make sure the `Token` and `RefreshToken` are securely stored (e.g., encrypted local storage).
/// 
/// 🔐 **Security Tips**
/// - Never expose the refresh token in browser console or URL.
/// - Use HTTPS to transmit all tokens.
/// 
/// 🔗 **Related Endpoints**
/// - `POST /api/auth/login`
/// - `POST /api/auth/register`
/// - `POST /api/auth/complete-registration`
/// 
// *****************************************************************************************
