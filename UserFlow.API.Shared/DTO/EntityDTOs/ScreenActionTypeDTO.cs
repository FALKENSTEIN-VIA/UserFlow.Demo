/// *****************************************************************************************
/// @file ScreenActionTypeDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Defines DTOs for screen action types used in screen interaction workflows.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 🧩 ScreenActionTypeDTO

/// <summary>
/// 🧩 Represents a screen action type (e.g., Click, Drag, Input).
/// </summary>
public class ScreenActionTypeDTO : BaseDTO
{
    /// <summary>
    /// 🏷️ Name of the action type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional description explaining the purpose or use case.
    /// </summary>
    public string? Description { get; set; }
}

#endregion

#region 🆕 ScreenActionTypeCreateDTO

/// <summary>
/// 🆕 DTO used when creating a new screen action type.
/// </summary>
public class ScreenActionTypeCreateDTO
{
    /// <summary>
    /// 🏷️ Name of the action type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional description explaining the purpose or use case.
    /// </summary>
    public string? Description { get; set; }
}

#endregion

#region ✏️ ScreenActionTypeUpdateDTO

/// <summary>
/// ✏️ DTO used when updating an existing screen action type.
/// </summary>
public class ScreenActionTypeUpdateDTO
{
    /// <summary>
    /// 🔑 ID of the screen action type to update.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🏷️ Updated name of the action type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional updated description.
    /// </summary>
    public string? Description { get; set; }
}

#endregion

#region 📥 ScreenActionTypeImportDTO

/// <summary>
/// 📥 DTO used when importing screen action types (e.g. from CSV).
/// </summary>
public class ScreenActionTypeImportDTO
{
    /// <summary>
    /// 🏷️ Name of the imported action type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional description for the imported action type.
    /// </summary>
    public string? Description { get; set; }
}

#endregion

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - `ScreenActionTypeDTO` is the main representation for use in API responses.
/// - Create, Update, and Import DTOs enable flexibility for UI and CSV tools.
/// - All types support optional `Description` for clarity and usability.
/// - These types are used by ScreenAction entities to define action semantics.
/// *****************************************************************************************
