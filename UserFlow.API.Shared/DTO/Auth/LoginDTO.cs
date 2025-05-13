/// *****************************************************************************************
/// @file LoginDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO for user login requests, containing credentials.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 🔐 DTO used for submitting user credentials during login.
/// </summary>
/// <remarks>
/// This object is sent by clients (e.g., MAUI or browser app) to authenticate a user.
/// The provided credentials will be validated, and if correct, a JWT and refresh token are returned.
/// </remarks>
public class LoginDTO
{
    /// <summary>
    /// 📧 Email address used to identify the user.
    /// </summary>
    /// <value>
    /// Must match the registered email of the user in the system.
    /// </value>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🔐 The password used to authenticate the user.
    /// </summary>
    /// <value>
    /// Passwords are compared using secure hashing through Identity framework.
    /// </value>
    public string Password { get; set; } = string.Empty;
}

/// @remarks
/// 🛠️ **Developer Notes**
/// - This DTO is used in the `/api/auth/login` endpoint.
/// - Both fields are required for successful login.
/// - Email is used as the username identifier.
/// - Password must match the stored hash in Identity.
/// 
/// 🔐 **Security Considerations**
/// - Transmit only over HTTPS.
/// - Avoid logging raw credentials under any circumstances.
/// 
/// 🔗 **Related Endpoint**
/// - `POST /api/auth/login`
///
/// ✅ **Validation**
/// - Client-side: Required fields, basic email format.
/// - Server-side: Full authentication check via `SignInManager`.
/// *****************************************************************************************
