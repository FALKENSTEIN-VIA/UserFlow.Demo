/// @file ScreenType.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Enum defining types of screens within the application.
/// @details
/// Provides distinct values to categorize different types of screens,
/// such as standard screens, popup overlays, and undefined/other types.
/// Useful for controlling UI behavior, navigation logic, or rendering styles.

namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Enum representing the various types of screens in the application.
/// </summary>
public enum ScreenType
{
    /// <summary>
    /// 🖥️ Standard screen type for regular application views.
    /// </summary>
    Screen,

    /// <summary>
    /// 🪟 Popup window type, usually used for modals or transient UI overlays.
    /// </summary>
    Popup,

    /// <summary>
    /// ❓ Undefined or custom screen type not matching standard categories.
    /// </summary>
    Other
}

/// @remarks
/// Developer Notes:
/// - 🧠 Used to drive conditional logic in UI rendering and navigation flow.
/// - 📌 Mapped via string or int depending on storage strategy (consider EF conversion).
/// - 🔧 Extendable with new types as the UI model evolves (e.g., Tab, Overlay, SplitView).
/// - 🛠️ Default fallback should be `Other` if value is unknown.
