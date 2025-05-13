/// @file Project.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Represents a user-owned Project entity in the application.
/// @details
/// The Project entity belongs to a specific user and can have multiple associated Screens and Notes.
/// It supports multi-tenancy (per user) and soft deletion through the BaseEntity class.

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Represents a project owned by a user.
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// 🏷 Name of the project (displayed in UI).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📝 Optional description for the project (e.g., goal or purpose).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🔄 Indicates if the project is shared across multiple users within a company.
    /// If false, only the owner (UserId) has access.
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// 📱 Screens belonging to this project.
    /// </summary>
    public ICollection<Screen> Screens { get; set; } = new List<Screen>();

    /// <summary>
    /// 📝 Notes assigned to this project.
    /// </summary>
    public ICollection<Note> Notes { get; set; } = new List<Note>();

    /// <summary>
    /// 🏢 Foreign key to the company that owns the project.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 👤 Foreign key to the user who owns the project.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 🏢 Navigation property to the owning company.
    /// </summary>
    public Company Company { get; set; } = null!;

    /// <summary>
    /// 👤 Navigation property to the owning user.
    /// </summary>
    public User User { get; set; } = null!;
}

/// @remarks
/// Developer Notes:
/// - 🔐 Projects are user-bound and support tenant separation via UserId and CompanyId.
/// - 🧠 Use `IsShared` to allow broader visibility within a company (e.g., Admins, Managers).
/// - 🗑 Soft delete is supported via IsDeleted from BaseEntity.
/// - ⚠️ Always configure navigation properties and delete behaviors in the EF Core configuration.
/// - 📦 Related entities: Screens (UI structures), Notes (annotations), Company, User.
