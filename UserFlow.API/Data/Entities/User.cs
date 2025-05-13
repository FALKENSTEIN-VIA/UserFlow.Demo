/// @file User.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Represents an application user extending ASP.NET Core IdentityUser with additional fields.
/// @details
/// Inherits from IdentityUser<long> (long-based primary key) and adds custom fields like Name.
/// Intended as a foundation for future user-specific extensions (e.g., roles, preferences, settings).

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Represents a user of the application, extending IdentityUser with a Name property.
/// </summary>
public class User : IdentityUser<long> // ⬅️ Extends IdentityUser with long as primary key
{
    /// <summary>
    /// 🧑 Full name of the user (for display purposes).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🏢 Foreign key to associate the user with a specific company (formerly tenant).
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// 🏢 Navigation property to access the associated company.
    /// </summary>
    [ForeignKey("CompanyId")]
    public Company? Company { get; set; } = null!;

    /// <summary>
    /// 🏢 Convenience property returning the associated company name (empty if null).
    /// </summary>
    public string? CompanyName => Company?.Name ?? string.Empty;

    /// <summary>
    /// 🆕 Indicates whether the user needs to set a password upon first login.
    /// </summary>
    public bool NeedsPasswordSetup { get; set; } = false;

    /// <summary>
    /// 🔐 Indicates whether the user is active and allowed to log in.
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// 🗑 Indicates whether the user is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 🎭 Role of the user (e.g., "Admin", "User", "Manager").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 🕓 Timestamp when the user was created (UTC).
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// 🕓 Timestamp when the user was last modified (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 👤 User ID of the creator.
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 👤 User ID of the last updater.
    /// </summary>
    public long? UpdatedBy { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 🔐 Used with ASP.NET Core Identity for login and role-based access control.
/// - 🏢 Supports multi-tenancy via CompanyId and optional navigation to Company.
/// - 🧠 Flags like NeedsPasswordSetup and IsActive enable onboarding logic and account management.
/// - 🗑 IsDeleted is used for soft deletion; query filters should exclude such users by default.
/// - ⚠️ Role value should match Identity role names and be validated accordingly.
/// - 📦 Suitable for further extension with preferences, audit logs, permissions, and external logins.
