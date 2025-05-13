/// *****************************************************************************************
/// @file CompleteRegistrationDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief DTO used by pre-created users to finalize their account registration.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO.Auth;

/// <summary>
/// 🔐 DTO used by pre-registered users to complete their registration by setting a password.
/// </summary>
/// <remarks>
/// This DTO is submitted to the `/api/auth/complete-registration` endpoint by users
/// who were created in advance by an administrator and now need to finalize their profile
/// by providing name, email and a secure password.
/// </remarks>
public class CompleteRegistrationDTO
{
    /// <summary>
    /// 🧾 The full name of the user (used for display in UI).
    /// </summary>
    /// <value>
    /// Typically used as a display name or user-friendly identifier in client applications.
    /// </value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 The email address the user wants to associate with their account.
    /// </summary>
    /// <value>
    /// This must match the pre-assigned email of the user in the system.
    /// </value>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🔐 The password the user chooses to secure their account.
    /// </summary>
    /// <value>
    /// A strong password that will be hashed and stored in the identity database.
    /// </value>
    public string Password { get; set; } = string.Empty;
}

/// @remarks
/// 🛠️ **Developer Notes**
/// - This DTO is used in admin-driven registration flows.
/// - The user must exist in the database with `NeedsPasswordSetup = true`.
/// - After setting the password, the account is marked as active and login is possible.
/// 
/// ✅ **Validation Notes**
/// - Password strength validation is applied on server-side.
/// - Email must match existing pre-registered account.
/// 
/// 🔗 **Related Endpoint**
/// - `POST /api/auth/complete-registration`
///
/// 🔐 **Security**
/// - Passwords are transmitted over HTTPS only.
/// - Never log the raw password, even in development.
/// *****************************************************************************************
