/// @file MappingHelper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Provides static extension methods for mapping between Entities and DTOs in UserFlowAPI.
/// @details
/// Enables clean and reusable conversions between database entities and transport DTOs (Data Transfer Objects),
/// following a fluent, null-safe approach for single and multiple mappings.

using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Helpers;

/// <summary>
/// 👉 ✨ Static helper class providing extension methods to map Entities to their corresponding DTOs.
/// </summary>
public static class MappingHelper
{
    #region 👉 ✨ Project Mappings

    /// <summary>
    /// 👉 ✨ Maps a single <see cref="Project"/> entity to a <see cref="ProjectDTO"/>.
    /// </summary>
    public static ProjectDTO ToDTO(this Project project)
    {
        return new ProjectDTO
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description
        };
    }

    /// <summary>
    /// 👉 ✨ Maps a collection of <see cref="Project"/> entities to a list of <see cref="ProjectDTO"/>.
    /// </summary>
    public static List<ProjectDTO> ToDTOs(this IEnumerable<Project>? projects)
    {
        return projects?.Select(p => p.ToDTO()).ToList() ?? [];
    }

    #endregion

    #region 👉 ✨ Screen Mappings

    /// <summary>
    /// 👉 ✨ Maps a single <see cref="Screen"/> entity to a <see cref="ScreenDTO"/>.
    /// </summary>
    public static ScreenDTO ToDTO(this Screen screen)
    {
        return new ScreenDTO
        {
            Id = screen.Id,
            Name = screen.Name,
            Identifier = screen.Identifier,
            Description = screen.Description ?? string.Empty,
            Type = screen.Type
        };
    }

    /// <summary>
    /// 👉 ✨ Maps a collection of <see cref="Screen"/> entities to a list of <see cref="ScreenDTO"/>.
    /// </summary>
    public static List<ScreenDTO> ToDTOs(this IEnumerable<Screen>? screens)
    {
        return screens?.Select(s => s.ToDTO()).ToList() ?? [];
    }

    #endregion

    #region 👉 ✨ ScreenAction Mappings

    /// <summary>
    /// 👉 ✨ Maps a single <see cref="ScreenAction"/> entity to a <see cref="ScreenActionDTO"/>.
    /// </summary>
    public static ScreenActionDTO ToDTO(this ScreenAction action)
    {
        return new ScreenActionDTO
        {
            Id = action.Id,
            ProjectId = action.ProjectId,
            ScreenId = action.ScreenId,
            Name = action.Name,
            EventAreaDefined = action.EventAreaDefined,
            EventX1 = action.EventX1,
            EventY1 = action.EventY1,
            EventX2 = action.EventX2,
            EventY2 = action.EventY2,
            EventDescription = action.EventDescription,
            SortIndex = action.SortIndex,
            SuccessorScreenId = action.SuccessorScreenId,
            ScreenActionTypeId = action.ScreenActionTypeId
        };
    }

    /// <summary>
    /// 👉 ✨ Maps a collection of <see cref="ScreenAction"/> entities to a list of <see cref="ScreenActionDTO"/>.
    /// </summary>
    public static List<ScreenActionDTO> ToDTOs(this IEnumerable<ScreenAction>? actions)
    {
        return actions?.Select(a => a.ToDTO()).ToList() ?? [];
    }

    #endregion

    #region 👉 ✨ ScreenActionType Mappings

    /// <summary>
    /// 👉 ✨ Maps a single <see cref="ScreenActionType"/> entity to a <see cref="ScreenActionTypeDTO"/>.
    /// </summary>
    public static ScreenActionTypeDTO ToDTO(this ScreenActionType actionType)
    {
        return new ScreenActionTypeDTO
        {
            Id = actionType.Id,
            Name = actionType.Name,
            Description = actionType.Description
        };
    }

    /// <summary>
    /// 👉 ✨ Maps a collection of <see cref="ScreenActionType"/> entities to a list of <see cref="ScreenActionTypeDTO"/>.
    /// </summary>
    public static List<ScreenActionTypeDTO> ToDTOs(this IEnumerable<ScreenActionType>? actionTypes)
    {
        return actionTypes?.Select(t => t.ToDTO()).ToList() ?? [];
    }

    #endregion

    #region 👉 ✨ Note Mappings

    /// <summary>
    /// 👉 ✨ Maps a single <see cref="Note"/> entity to a <see cref="NoteDTO"/>.
    /// </summary>
    public static NoteDTO ToDTO(this Note note)
    {
        return new NoteDTO
        {
            Id = note.Id,
            UserId = note.UserId,
            ProjectId = note.ProjectId ?? 0,
            ScreenId = note.ScreenId ?? 0,
            Title = note.Title,
            Content = note.Content
        };
    }

    /// <summary>
    /// 👉 ✨ Maps a collection of <see cref="Note"/> entities to a list of <see cref="NoteDTO"/>.
    /// </summary>
    public static List<NoteDTO> ToDTOs(this IEnumerable<Note>? notes)
    {
        return notes?.Select(n => n.ToDTO()).ToList() ?? [];
    }

    #endregion

    #region 👉 ✨ User Mappings

    /// <summary>
    /// 👉 ✨ Maps a single <see cref="User"/> entity to a <see cref="UserDTO"/>.
    /// </summary>
    public static UserDTO ToDTO(this User user)
    {
        return new UserDTO
        {
            Id = user.Id,
            Name = user.Name ?? string.Empty,
            Email = user.Email ?? string.Empty
        };
    }

    /// <summary>
    /// 👉 ✨ Maps a collection of <see cref="User"/> entities to a list of <see cref="UserDTO"/>.
    /// </summary>
    public static List<UserDTO> ToDTOs(this IEnumerable<User>? users)
    {
        return users?.Select(u => u.ToDTO()).ToList() ?? [];
    }

    #endregion
}

/// @remarks
/// Developer Notes:
/// - 🧩 All mapping methods are implemented as static extension methods for clean syntax and reusability.
/// - 🧼 Null input collections are safely handled with fallback to empty lists.
/// - 📦 These methods decouple API responses from EF Core entities, making the application more maintainable.
/// - ✨ Keep DTOs minimal — never expose internal navigation properties or metadata.
/// - 🧠 Extend this helper with reverse mappings (DTO → Entity) if needed for creation or updates.
