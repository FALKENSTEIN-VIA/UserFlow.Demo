/// *****************************************************************************************
/// @file RegisterDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO for new user registration including name, email, and role.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 📝 DTO used to register a new user.
/// </summary>
/// <remarks>
/// This DTO is typically submitted by an administrator or during company registration
/// to pre-create a user account. The user is later expected to complete the setup process
/// (e.g., by setting a password).
/// </remarks>
public class RegisterDTO
{
    /// <summary>
    /// 👤 Full name of the user (for display purposes).
    /// </summary>
    /// <value>
    /// Example: "Jane Doe"
    /// </value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email address of the user (used as login identifier).
    /// </summary>
    /// <value>
    /// Must be unique and valid. Example: "jane.doe@example.com"
    /// </value>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🎭 Role to assign to the user (e.g., "User", "Manager", "Admin").
    /// </summary>
    /// <value>
    /// Should match an existing role defined in the system.
    /// </value>
    public string Role { get; set; } = string.Empty;
}

/// @remarks
/// 🛠️ **Developer Notes**
/// - Used in `AuthController.Register(...)` and `CompanyController.RegisterCompany(...)`.
/// - The user is created with `NeedsPasswordSetup = true` and no initial password.
/// - Valid roles are typically checked on the server side against a predefined list.
///
/// 🔐 **Security Considerations**
/// - The email must be validated to prevent duplicates or malformed entries.
/// - Role assignment should be restricted to allowed values (e.g., avoid granting "Admin" by default).
/// - Registration logic should ensure that only verified company owners or admins can create users.
/// *****************************************************************************************
