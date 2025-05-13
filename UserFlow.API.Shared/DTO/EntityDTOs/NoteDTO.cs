/// *****************************************************************************************
/// @file NoteDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Defines DTOs for managing notes linked to projects, screens, and actions.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 📝 NoteDTO

/// <summary>
/// 📝 Represents a user-created note, optionally linked to a project, screen, and screen action.
/// </summary>
public class NoteDTO : BaseDTO
{
    /// <summary>
    /// 🏷️ Title of the note.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Full content of the note.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 🧪 Project name for display (optional).
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// 🖼️ Screen name for display (optional).
    /// </summary>
    public string? ScreenName { get; set; }

    /// <summary>
    /// 🏢 Company that owns the note.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 👤 ID of the user who created the note.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 🔗 Optional ID of the related project.
    /// </summary>
    public long? ProjectId { get; set; }

    /// <summary>
    /// 🖼️ Optional ID of the related screen.
    /// </summary>
    public long? ScreenId { get; set; }

    /// <summary>
    /// 🎬 Optional ID of the related screen action.
    /// </summary>
    public long? ScreenActionId { get; set; }
}

#endregion

#region 🆕 NoteCreateDTO

/// <summary>
/// 🆕 DTO for creating a new note.
/// </summary>
public class NoteCreateDTO
{
    /// <summary>
    /// 🏷️ Title of the note.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Content of the note.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 🏢 Owning company.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 👤 Creator user ID.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 🔗 Optional project ID.
    /// </summary>
    public long? ProjectId { get; set; }

    /// <summary>
    /// 🖼️ Optional screen ID.
    /// </summary>
    public long? ScreenId { get; set; }

    /// <summary>
    /// 🎬 Optional screen action ID.
    /// </summary>
    public long? ScreenActionId { get; set; }
}

#endregion

#region ✏️ NoteUpdateDTO

/// <summary>
/// ✏️ DTO for updating an existing note.
/// </summary>
public class NoteUpdateDTO
{
    /// <summary>
    /// 🆔 ID of the note to update.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🏷️ Updated title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Updated content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 🏢 Owning company.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 👤 User performing the update.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 🔗 Optional new project link.
    /// </summary>
    public long? ProjectId { get; set; }

    /// <summary>
    /// 🖼️ Optional new screen link.
    /// </summary>
    public long? ScreenId { get; set; }

    /// <summary>
    /// 🎬 Optional new screen action link.
    /// </summary>
    public long? ScreenActionId { get; set; }
}

#endregion

#region 📥 NoteImportDTO

/// <summary>
/// 📥 DTO used for importing notes from external sources (e.g. CSV).
/// </summary>
public class NoteImportDTO
{
    /// <summary>
    /// 🏷️ Title of the imported note.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Content of the imported note.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 🏢 Owning company.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 👤 User associated with this note.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 🔗 Project link (if applicable).
    /// </summary>
    public long? ProjectId { get; set; }

    /// <summary>
    /// 🖼️ Screen link (if applicable).
    /// </summary>
    public long? ScreenId { get; set; }

    /// <summary>
    /// 🎬 Screen action link (if applicable).
    /// </summary>
    public long? ScreenActionId { get; set; }
}

#endregion

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - Notes can be optionally linked to projects, screens, and screen actions.
/// - DTOs follow a consistent structure for Create, Update, and Import scenarios.
/// - Used across WebAPI endpoints and CSV import/export services.
/// - Each note is assigned to a company and user to support multi-tenancy.
/// - BaseDTO provides CreatedAt, UpdatedAt, and IsDeleted flags (via inheritance).
/// *****************************************************************************************
