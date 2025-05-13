/// *****************************************************************************************
/// @file ProjectDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Defines DTOs for managing user projects.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 📁 ProjectDTO

/// <summary>
/// 📁 Represents a user project, which may be shared or private.
/// </summary>
public class ProjectDTO : BaseDTO
{
    /// <summary>
    /// 🏷️ Project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional description of the project.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🔁 Indicates if the project is shared across users.
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// 👤 ID of the user who created the project.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 🙋 Name of the user who owns the project.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 🏢 ID of the company that owns the project.
    /// </summary>
    public long CompanyId { get; set; }

    /// <summary>
    /// 🙋 Name of the company who owns the project.
    /// </summary>
    public string? CompanyName { get; set; }


    /// <summary>
    /// 🧪 Returns a formatted string representation of the project.
    /// </summary>
    public override string ToString() => $"{Id} - {Name}";
}

#endregion

#region 🆕 ProjectCreateDTO

/// <summary>
/// 🆕 DTO for creating a new project.
/// </summary>
public class ProjectCreateDTO
{
    /// <summary>
    /// 🏷️ Name of the new project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Optional description for the project.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🔁 Indicates if the project should be shared.
    /// </summary>
    public bool IsShared { get; set; } = false;

    /// <summary>
    /// 🏢 Company ID to associate the project with.
    /// </summary>
    public long CompanyId { get; set; }
}

#endregion

#region ✏️ ProjectUpdateDTO

/// <summary>
/// ✏️ DTO for updating an existing project.
/// </summary>
public class ProjectUpdateDTO
{
    /// <summary>
    /// 🆔 ID of the project to update.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🏷️ New name for the project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Updated description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🔁 Indicates if the project remains shared.
    /// </summary>
    public bool IsShared { get; set; } = false;
}

#endregion

#region 📥 ProjectImportDTO

/// <summary>
/// 📥 DTO for importing a project via file (e.g. CSV).
/// </summary>
public class ProjectImportDTO
{
    /// <summary>
    /// 🏷️ Name of the imported project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🧾 Description of the imported project.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 🔁 Whether the imported project is shared.
    /// </summary>
    public bool IsShared { get; set; }
}

#endregion

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - DTOs follow a clear Create/Update/Import pattern.
/// - Projects are always associated with a company and optionally shared.
/// - Multi-tenancy is supported via CompanyId and UserId (in ProjectDTO).
/// - `IsShared` flag is used to distinguish between private and shared projects.
/// - `ToString()` aids debugging and UI display.
/// *****************************************************************************************
