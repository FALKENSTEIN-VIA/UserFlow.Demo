/// @file ScreenAction.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Represents an interactive action (event) performed on a screen.
/// @details
/// The ScreenAction entity models user-triggered interactions on screens,
/// including optional event areas (coordinates), navigation targets (SuccessorScreen),
/// and categorization by action types (ScreenActionType).

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Represents an interactive action performed on a screen.
/// </summary>
public class ScreenAction : BaseEntity
{
    /// <summary>
    /// 🏷 Display name of the action.
    /// </summary>
    [Required, MaxLength(20)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🔲 Indicates whether the event area (coordinate boundaries) is defined.
    /// </summary>
    public bool EventAreaDefined { get; set; }

    /// <summary>
    /// 📍 X-coordinate of the event area start point (optional).
    /// </summary>
    public int? EventX1 { get; set; }

    /// <summary>
    /// 📍 Y-coordinate of the event area start point (optional).
    /// </summary>
    public int? EventY1 { get; set; }

    /// <summary>
    /// 📍 X-coordinate of the event area end point (optional).
    /// </summary>
    public int? EventX2 { get; set; }

    /// <summary>
    /// 📍 Y-coordinate of the event area end point (optional).
    /// </summary>
    public int? EventY2 { get; set; }

    /// <summary>
    /// 📝 Optional description providing context for the event.
    /// </summary>
    [MaxLength(120)]
    public string? EventDescription { get; set; }

    /// <summary>
    /// 🔢 Sort order index among actions within the same screen.
    /// </summary>
    public int SortIndex { get; set; }

    /// <summary>
    /// 👤 ID of the user who owns this action.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 👤 Navigation property to the owning user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// 📁 ID of the project this action belongs to.
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 📁 Navigation property to the related project.
    /// </summary>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// 📱 ID of the screen where this action is performed.
    /// </summary>
    public long ScreenId { get; set; }

    /// <summary>
    /// 📱 Navigation property to the screen where the action occurs.
    /// </summary>
    public Screen Screen { get; set; } = null!;

    /// <summary>
    /// 🔄 Optional ID of the successor screen navigated to after this action.
    /// </summary>
    public long? SuccessorScreenId { get; set; }

    /// <summary>
    /// 🔄 Navigation property to the successor screen (optional).
    /// </summary>
    public Screen? SuccessorScreen { get; set; }

    /// <summary>
    /// 🧩 ID of the type categorizing this action (e.g., Click, Submit).
    /// </summary>
    public long ScreenActionTypeId { get; set; }

    /// <summary>
    /// 🧩 Navigation property to the ScreenActionType.
    /// </summary>
    public ScreenActionType ScreenActionType { get; set; } = null!;

    /// <summary>
    /// 🏢 Company ID for tenant filtering and ownership.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 🏢 Navigation property to the owning company.
    /// </summary>
    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;

    /// <summary>
    /// 📛 Returns the name of the action as its string representation.
    /// </summary>
    /// <returns>Action name.</returns>
    public override string ToString() => Name;
}

/// @remarks
/// Developer Notes:
/// - 🎬 Represents UI-driven user interaction (e.g., clicks, drags, submits).
/// - 🔗 Connected to Screen, Project, User, ActionType and optionally to a SuccessorScreen.
/// - 🗺️ Coordinate fields (EventX1..Y2) allow drawing bounding boxes on the screen.
/// - 🧠 SortIndex helps ordering actions visually or logically.
/// - 🧪 Used in behavior simulation, UX testing, and action tracing.
/// - 🏢 CompanyId supports multi-tenancy and is essential for filtering and seeding.
/// - ⚠️ All relations must be explicitly configured in EF Core to prevent shadow properties.
