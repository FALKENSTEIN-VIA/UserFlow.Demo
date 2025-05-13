/// @file ScreenMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides a mapping expression to convert Screen entities into ScreenDTOs.
/// @details
/// Defines a LINQ expression for projecting <see cref="Screen"/> entities into <see cref="ScreenDTO"/>s,
/// including optional navigation information like project name.

using System.Linq.Expressions;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Mappers;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="Screen"/> entities into <see cref="ScreenDTO"/>s.
/// </summary>
public static class ScreenMapper
{
    /// <summary>
    /// 👉 ✨ Returns a compiled expression to project a <see cref="Screen"/> into a <see cref="ScreenDTO"/>.
    /// </summary>
    /// <returns>An <see cref="Expression"/> usable directly in EF Core LINQ queries.</returns>
    public static Expression<Func<Screen, ScreenDTO>> ToScreenDto()
    {
        return screen => new ScreenDTO
        {
            Id = screen.Id,                                 // 🔑 Unique screen ID
            Name = screen.Name,                             // 🏷️ Display name of the screen
            Identifier = screen.Identifier,                 // 🆔 Unique business identifier
            Type = screen.Type,                             // 🎨 Screen type (optional enum/string)
            Description = screen.Description ?? string.Empty, // 📝 Description (default to empty string if null)
            UserId = screen.UserId,                         // 👤 Creator or owner of the screen
            ProjectId = screen.ProjectId,                   // 📦 Associated project
            CompanyId = screen.CompanyId,                   // 🏢 Associated company
            ProjectName = screen.Project != null
                ? screen.Project.Name                       // 🏷️ Include project name if navigation loaded
                : string.Empty
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Uses a compiled projection for efficient EF Core translation to SQL.
/// - 🔗 ProjectName is conditionally included to avoid null references in the DTO.
/// - 🧼 The output DTO is safe for client consumption and avoids deep object graphs.
/// - 🧠 Extend this mapper if screen types or related stats are needed for display.
/// - 🧩 Designed for use in filtered or paginated queries where performance matters.
