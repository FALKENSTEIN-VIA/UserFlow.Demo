/// *****************************************************************************************
/// @file UserDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO definitions for user data including admin creation and update operations.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 👤 UserDTO

/// <summary>
/// 👤 Represents a user in the system.
/// </summary>
public class UserDTO : BaseDTO
{
    /// <summary>
    /// 🧑 Full name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🛡️ Role assigned to the user (e.g., Admin, User).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 🔐 Indicates whether the user needs to set a password before login.
    /// </summary>
    public bool NeedsPasswordSetup { get; set; }

    /// <summary>
    /// ✅ Indicates whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 🏢 Optional company ID the user is assigned to.
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// 🏷️ Optional company name (for display).
    /// </summary>
    public string? CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 🏢 Optional company details (can be null).
    /// </summary>
    public CompanyDTO? Company { get; set; }

    /// <summary>
    /// 📛 Returns user info as string for debugging or UI.
    /// </summary>
    public override string ToString() => $"{Id} - {Name}";
}

#endregion

#region 🆕 CreateUserByAdminDTO

/// <summary>
/// 🆕 DTO used by an admin to create a new user manually.
/// </summary>
public class CreateUserByAdminDTO
{
    /// <summary>
    /// 🧑 Full name of the new user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email of the new user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🛡️ Role to assign (e.g., User, Admin, Manager).
    /// </summary>
    public string Role { get; set; } = "User";
}

#endregion

#region ✏️ UpdateUserDTO

/// <summary>
/// ✏️ DTO used to update user information.
/// </summary>
public class UpdateUserDTO
{
    /// <summary>
    /// 🔑 ID of the user to update.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🧑 Updated name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Updated email of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

#endregion

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - UserDTO is used in API responses to display full user data.
/// - CreateUserByAdminDTO is only used by Admin roles to create new accounts.
/// - UpdateUserDTO allows editing a user’s name and email (no role change).
/// - Role values must be validated against the list of allowed roles in the API or client.
/// *****************************************************************************************
