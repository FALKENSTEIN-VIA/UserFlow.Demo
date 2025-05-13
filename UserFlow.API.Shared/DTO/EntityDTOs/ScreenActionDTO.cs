/// *****************************************************************************************
/// @file ScreenActionDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Defines DTOs for managing screen actions in the UserFlow application.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 🎯 ScreenActionDTO

/// <summary>
/// 🎯 Represents a user-defined screen action with optional hotspot coordinates.
/// </summary>
public class ScreenActionDTO : BaseDTO
{
    /// <summary>
    /// 🏷️ Name of the action (e.g., "Click Submit").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional description of the action's behavior or purpose.
    /// </summary>
    public string? EventDescription { get; set; }

    /// <summary>
    /// 🔲 Indicates whether a clickable area is defined.
    /// </summary>
    public bool EventAreaDefined { get; set; }

    /// <summary>
    /// 📍 Top-left X coordinate of the event area.
    /// </summary>
    public int? EventX1 { get; set; }

    /// <summary>
    /// 📍 Top-left Y coordinate of the event area.
    /// </summary>
    public int? EventY1 { get; set; }

    /// <summary>
    /// 📍 Bottom-right X coordinate of the event area.
    /// </summary>
    public int? EventX2 { get; set; }

    /// <summary>
    /// 📍 Bottom-right Y coordinate of the event area.
    /// </summary>
    public int? EventY2 { get; set; }

    /// <summary>
    /// 🧮 Defines the sort order of this action.
    /// </summary>
    public int SortIndex { get; set; }

    /// <summary>
    /// 🖼️ ID of the screen this action belongs to.
    /// </summary>
    public long ScreenId { get; set; }

    /// <summary>
    /// 🧩 Type of the screen action (e.g., Click, Drag).
    /// </summary>
    public long ScreenActionTypeId { get; set; }

    /// <summary>
    /// 📂 ID of the project to which this action belongs.
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 🔁 Optional: ID of the screen that follows this action.
    /// </summary>
    public long? SuccessorScreenId { get; set; }

    /// <summary>
    /// 👤 User ID of the action creator.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 🏢 Company ID to support multi-tenancy.
    /// </summary>
    public long CompanyId { get; set; }
}

#endregion

#region 🆕 ScreenActionCreateDTO

/// <summary>
/// 🆕 DTO for creating a new screen action.
/// </summary>
public class ScreenActionCreateDTO
{
    public string Name { get; set; } = string.Empty;
    public string? EventDescription { get; set; }
    public bool EventAreaDefined { get; set; }
    public int? EventX1 { get; set; }
    public int? EventY1 { get; set; }
    public int? EventX2 { get; set; }
    public int? EventY2 { get; set; }
    public int SortIndex { get; set; }
    public long ScreenId { get; set; }
    public long ScreenActionTypeId { get; set; }
    public long ProjectId { get; set; }
    public long? SuccessorScreenId { get; set; }
}

#endregion

#region ✏️ ScreenActionUpdateDTO

/// <summary>
/// ✏️ DTO for updating an existing screen action.
/// </summary>
public class ScreenActionUpdateDTO
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EventDescription { get; set; }
    public bool EventAreaDefined { get; set; }
    public int? EventX1 { get; set; }
    public int? EventY1 { get; set; }
    public int? EventX2 { get; set; }
    public int? EventY2 { get; set; }
    public int SortIndex { get; set; }
    public long ScreenId { get; set; }
    public long ScreenActionTypeId { get; set; }
    public long ProjectId { get; set; }
    public long? SuccessorScreenId { get; set; }
}

#endregion

#region 📥 ScreenActionImportDTO

/// <summary>
/// 📥 DTO for importing a screen action from external data (e.g. CSV).
/// </summary>
public class ScreenActionImportDTO
{
    public string Name { get; set; } = string.Empty;
    public string? EventDescription { get; set; }
    public bool EventAreaDefined { get; set; }
    public int? EventX1 { get; set; }
    public int? EventY1 { get; set; }
    public int? EventX2 { get; set; }
    public int? EventY2 { get; set; }
    public int SortIndex { get; set; }
    public long ScreenId { get; set; }
    public long ScreenActionTypeId { get; set; }
    public long? SuccessorScreenId { get; set; }
    public long ProjectId { get; set; }
}

#endregion

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - All DTOs support the ScreenAction workflow: Create, Update, Import.
/// - `EventAreaDefined` activates the use of X/Y coordinates.
/// - `SuccessorScreenId` enables flow mapping (screen transitions).
/// - `ScreenActionDTO` includes UserId and CompanyId for multi-tenancy support.
/// - Actions are ordered using `SortIndex`.
/// *****************************************************************************************
