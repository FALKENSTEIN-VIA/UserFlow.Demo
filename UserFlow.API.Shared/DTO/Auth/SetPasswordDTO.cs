/// *****************************************************************************************
/// @file SetPasswordDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO for setting a password after pre-registration.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO.Auth;

/// <summary>
/// 🔐 DTO used by a pre-created user to set their password during first login.
/// </summary>
/// <remarks>
/// Used in flows where an admin or system pre-registers users without passwords,
/// and the user completes the setup later via a secure first login process.
/// </remarks>
public class SetPasswordDTO
{
    /// <summary>
    /// 👤 Full name of the user (optional, used for display or finalizing profile setup).
    /// </summary>
    /// <value>
    /// Example: "Max Mustermann"
    /// </value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email address used to identify the user account.
    /// </summary>
    /// <value>
    /// Must match an existing user created without password.
    /// </value>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🔑 The password the user wants to set.
    /// </summary>
    /// <value>
    /// Should meet system password complexity requirements.
    /// </value>
    public string Password { get; set; } = string.Empty;
}

/// @remarks
/// 🛠️ **Developer Notes**
/// - This DTO is submitted to `AuthController.SetPassword(...)` endpoint.
/// - The backend should validate that the user exists and still has `NeedsPasswordSetup = true`.
/// - After setting the password, the system typically marks the user as active (`IsActive = true`)
///   and clears the `NeedsPasswordSetup` flag.
///
/// 🔐 **Security Considerations**
/// - Ensure password meets complexity requirements.
/// - Use secure password handling and hashing mechanisms via ASP.NET Core Identity.
/// - Rate-limit or monitor this endpoint to prevent abuse.
/// *****************************************************************************************
