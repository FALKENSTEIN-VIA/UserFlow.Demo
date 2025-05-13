/// @file ScreenActionMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Defines mapping expressions for converting ScreenAction entities to DTOs.
/// @details
/// Provides a projection that converts <see cref="ScreenAction"/> entities into <see cref="ScreenActionDTO"/>s
/// for use in API responses, ensuring lightweight, structured output suitable for client applications.

using System.Linq.Expressions;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Mappers;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="ScreenAction"/> entities into <see cref="ScreenActionDTO"/>s.
/// </summary>
public static class ScreenActionMapper
{
    /// <summary>
    /// 👉 ✨ Returns a compiled projection from <see cref="ScreenAction"/> to <see cref="ScreenActionDTO"/>.
    /// </summary>
    /// <returns>An EF Core-compatible <see cref="Expression"/> that maps entity fields to DTO fields.</returns>
    public static Expression<Func<ScreenAction, ScreenActionDTO>> ToScreenActionDto()
    {
        return action => new ScreenActionDTO
        {
            Id = action.Id,                           // 🔑 Unique action identifier
            Name = action.Name,                       // 🏷️ Action label/name
            EventDescription = action.EventDescription, // 📝 Optional description for the event
            EventAreaDefined = action.EventAreaDefined, // 📐 Flag indicating area definition
            EventX1 = action.EventX1,                 // 📍 X1 position
            EventY1 = action.EventY1,                 // 📍 Y1 position
            EventX2 = action.EventX2,                 // 📍 X2 position
            EventY2 = action.EventY2,                 // 📍 Y2 position
            SortIndex = action.SortIndex,             // 🔢 Used for ordering in UI
            ScreenId = action.ScreenId,               // 🖥️ ID of the parent screen
            ScreenActionTypeId = action.ScreenActionTypeId, // 🔘 Type of the action
            ProjectId = action.ProjectId,             // 📌 Related project ID
            SuccessorScreenId = action.SuccessorScreenId, // 🔄 ID of the target screen (for navigation)
            UserId = action.UserId,                   // 👤 Creator/owner of the action
            CompanyId = action.CompanyId              // 🏢 Belonging company
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Efficient compiled projection for use in queries, pagination, and exports.
/// - 🧼 Avoids loading navigation properties — this DTO is flat and safe for public clients.
/// - 📍 Coordinates (X1–Y2) define a clickable/touchable area on the screen.
/// - 🔄 SuccessorScreenId links to the next screen (optional navigation logic).
/// - 🧠 Extend this mapper when you need more context (e.g. ScreenName, ActionTypeLabel).
