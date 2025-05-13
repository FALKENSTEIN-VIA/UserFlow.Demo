/// *****************************************************************************************
/// @file ScreenDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTO definitions for screens in a project (e.g., UI screens, views).
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 🖥️ ScreenDTO

/// <summary>
/// 🖥️ Represents a screen in a user flow or application project.
/// </summary>
public class ScreenDTO : BaseDTO
{
    /// <summary>
    /// 🏷️ Name of the screen (e.g., "Login Screen").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🆔 Technical or unique identifier of the screen.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 🧩 Type of the screen (e.g., "Form", "Dialog", "Page").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional description of the screen.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 👤 ID of the user who created the screen.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 📁 ID of the associated project.
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 📝 Name of the associated project (for display purposes).
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 🏢 ID of the company owning the screen.
    /// </summary>
    public long CompanyId { get; set; }
}

#endregion

#region 🆕 ScreenCreateDTO

/// <summary>
/// 🆕 DTO used to create a new screen.
/// </summary>
public class ScreenCreateDTO
{
    /// <summary>
    /// 🏷️ Name of the screen.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🆔 Unique identifier of the screen.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 🧩 Type of the screen (e.g., "Main", "Popup").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional screen description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 📁 ID of the project the screen belongs to.
    /// </summary>
    public long ProjectId { get; set; }
}

#endregion

#region ✏️ ScreenUpdateDTO

/// <summary>
/// ✏️ DTO used to update an existing screen.
/// </summary>
public class ScreenUpdateDTO
{
    /// <summary>
    /// 🔑 ID of the screen to update.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🏷️ Updated name of the screen.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🆔 Updated identifier.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 🧩 Updated type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Updated description.
    /// </summary>
    public string? Description { get; set; }
}

#endregion

#region 📥 ScreenImportDTO

/// <summary>
/// 📥 DTO used for importing screens from external sources (e.g., CSV).
/// </summary>
public class ScreenImportDTO
{
    /// <summary>
    /// 🏷️ Name of the imported screen.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🆔 Unique identifier of the imported screen.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Description for the imported screen.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🧩 Screen type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 📁 Associated project ID.
    /// </summary>
    public long ProjectId { get; set; }
}

#endregion

#region ✅ ValidationResponseDTO

/// <summary>
/// ✅ Represents the result of a create/update operation with validation feedback.
/// </summary>
public class ValidationResponseDTO
{
    /// <summary>
    /// ✅ Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 💬 A user-facing message indicating the result (success or validation issues).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional list of validation errors (e.g. field names and error messages).
    /// </summary>
    public List<ValidationErrorDTO> Errors { get; set; } = new();
}

/// <summary>
/// 📌 Represents a single validation error.
/// </summary>
public class ValidationErrorDTO
{
    /// <summary>
    /// 🔑 The name of the property or field that failed validation.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 📛 The corresponding validation error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

#endregion


/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - ScreenDTO is used for displaying screen metadata in the frontend.
/// - ScreenCreateDTO and ScreenUpdateDTO are used in forms or modals.
/// - ScreenImportDTO allows importing screen definitions via CSV/Excel.
/// - Always ensure ProjectId is validated on import to maintain data integrity.
/// *****************************************************************************************
