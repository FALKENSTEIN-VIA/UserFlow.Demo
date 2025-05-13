/// @file ScreenActionType.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Represents the type/category of a user action performed on a screen (e.g., click, submit, swipe).
/// @details
/// The ScreenActionType entity is used to classify user-triggered interactions into specific categories,
/// enabling structured filtering, reporting, UI behavior differentiation, and analytics.

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Defines the category or type of an action triggered on a screen.
/// </summary>
public class ScreenActionType : BaseEntity
{
    /// <summary>
    /// 🏷 Name of the action type (e.g., "Click", "Submit", "Swipe").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📝 Optional description providing more detail about the action type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🎬 Collection of screen actions associated with this action type.
    /// </summary>
    public ICollection<ScreenAction> ScreenActions { get; set; } = new List<ScreenAction>();
}

/// @remarks
/// Developer Notes:
/// - 🧩 ScreenActionTypes are used to semantically classify interactions.
/// - 📊 Useful for filtering screen actions by intent (e.g., navigation, input, dismissal).
/// - 📁 Related to ScreenAction via one-to-many relationship.
/// - 🧠 Consider enforcing uniqueness on the Name property to avoid ambiguity.
/// - 🔄 Extensible with future metadata (e.g., color, icon, behavior flags).
/// - 🗑 Inherits soft delete via BaseEntity (IsDeleted).
