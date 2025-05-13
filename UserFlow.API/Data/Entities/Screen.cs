using System.ComponentModel.DataAnnotations.Schema;

/// @file Screen.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Represents a screen (e.g., page, UI element) within a user project.
/// @details
/// The Screen entity models a logical UI element linked to a project and optionally to a user.
/// Supports soft deletion (via BaseEntity), notes (annotations), and user interactions (ScreenActions).

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Represents a screen within a project.
/// </summary>
public class Screen : BaseEntity
{
    /// <summary>
    /// 🆔 Unique identifier or technical name of the screen (e.g., internal key).
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 🧩 Logical type of the screen (e.g., Menu, ContentPage, Popup).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 📝 Optional human-readable description of the screen.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🖼️ Display name of the screen shown to the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 👤 The ID of the user who owns this screen.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 👤 Navigation property to the owning user (optional).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// 📁 The ID of the project this screen belongs to.
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 📁 Navigation property to the parent project.
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// 🎬 Collection of user actions associated with this screen (ScreenActions).
    /// </summary>
    public ICollection<ScreenAction> ScreenActions { get; set; } = new List<ScreenAction>();

    /// <summary>
    /// 📝 Collection of notes attached to this screen.
    /// </summary>
    public ICollection<Note> Notes { get; set; } = new List<Note>();

    /// <summary>
    /// 🏢 ID of the company this screen is assigned to.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 🏢 Navigation property to the company.
    /// </summary>
    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;
}

/// @remarks
/// Developer Notes:
/// - 📱 A screen is a logical UI node and may contain multiple ScreenActions and Notes.
/// - 🔐 Each screen is owned by a user (UserId) and belongs to a project (ProjectId).
/// - 🏢 CompanyId is used for tenant separation; ensure query filters respect it.
/// - 🗑 Inherits soft delete support via BaseEntity (IsDeleted).
/// - ⚠️ All navigation properties must be explicitly configured in EF Core to avoid shadow properties.
/// - 🔄 Can be extended to include layout metadata, design info or analytics.
