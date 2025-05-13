/// @file Note.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Represents a note attached to a screen, project, or user within the UserFlow system.
/// @details
/// The Note entity can be flexibly linked to a User, Project, or Screen. 
/// Supports soft deletion and text-based annotations.

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 📝 Represents a comment, reminder or annotation in the system.
/// Notes can be linked to users, projects, screens and screen actions.
/// </summary>
public class Note : BaseEntity
{
    /// <summary>
    /// 🏷 Title of the note (e.g., "Reminder", "Todo", "Feedback").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 📄 Full text content of the note.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// ────────────────
    /// 🔗 Required Foreign Keys
    /// ────────────────

    /// <summary>
    /// 🏢 ID of the company this note belongs to.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 👤 ID of the user who created the note.
    /// </summary>
    public long UserId { get; set; }

    /// ────────────────
    /// 🔗 Optional Foreign Keys
    /// ────────────────

    /// <summary>
    /// 📁 Optional link to a project.
    /// </summary>
    public long? ProjectId { get; set; }

    /// <summary>
    /// 📱 Optional link to a screen.
    /// </summary>
    public long? ScreenId { get; set; }

    /// <summary>
    /// 🎬 Optional link to a screen action.
    /// </summary>
    public long? ScreenActionId { get; set; }

    /// ────────────────
    /// 🔁 Navigation Properties
    /// ────────────────

    /// <summary>
    /// 🏢 Company to which this note belongs.
    /// </summary>
    public Company Company { get; set; } = null!;

    /// <summary>
    /// 👤 User who created the note.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// 📁 Project context of the note (if any).
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// 📱 Screen context of the note (if any).
    /// </summary>
    public Screen? Screen { get; set; }

    /// <summary>
    /// 🎬 Screen action context of the note (if any).
    /// </summary>
    public ScreenAction? ScreenAction { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 📝 Notes are user-generated entries used for annotations, comments, or feedback.
/// - 🔗 Required: CompanyId and UserId must always be present.
/// - 📎 Optional links: ProjectId, ScreenId, and ScreenActionId (only one or multiple may apply).
/// - 🗑 Supports soft deletion via IsDeleted from BaseEntity.
/// - 📌 Ideal for traceability of actions and collaborative work.
/// - ⚠️ Ensure foreign key relationships are properly configured to avoid EF shadow properties.
