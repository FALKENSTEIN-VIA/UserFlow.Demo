/// @file ProjectMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides a mapping expression from Project entities to ProjectDTOs.
/// @details
/// Defines a lightweight projection for converting <see cref="Project"/> entities into transport-friendly
/// <see cref="ProjectDTO"/> objects that are suitable for API responses and client display.

using System.Linq.Expressions;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Mappers;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="Project"/> entities into <see cref="ProjectDTO"/>s.
/// </summary>
public static class ProjectMapper
{
    /// <summary>
    /// 👉 ✨ Returns a projection expression from <see cref="Project"/> to <see cref="ProjectDTO"/>.
    /// </summary>
    /// <returns>An EF Core-compatible <see cref="Expression"/> for project DTO mapping.</returns>
    public static Expression<Func<Project, ProjectDTO>> ToProjectDto()
    {
        return project => new ProjectDTO
        {
            Id = project.Id,                    // 🔑 Unique project ID
            Name = project.Name,                // 🏷️ Project name
            Description = project.Description,  // 📝 Optional description
            IsShared = project.IsShared,        // 🔁 Indicates if project is shared across users
            UserId = project.UserId,            // 👤 Owner user ID
            CompanyId = project.CompanyId,      // 🏢 Owning company ID
            UserName = project.User.UserName,   // 🙋 Name of the owning user (via navigation)
            CompanyName = project.Company.Name  // 🏢 Name of the owning company (via navigation)
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Uses `Expression<Func<>>` for LINQ-to-SQL projection and optimal performance.
/// - 🚫 Does not include navigation properties — keep DTOs lightweight for listing views.
/// - 🧠 Suitable for use in queries like pagination, filtering, or exporting project lists.
/// - ✨ You can expand the DTO later to include statistics or relationships (e.g., screen count).
