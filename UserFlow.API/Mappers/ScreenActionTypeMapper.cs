/// @file ScreenActionTypeMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Defines a mapping expression to convert ScreenActionType entities to DTOs.
/// @details
/// Provides a compiled LINQ expression for projecting <see cref="ScreenActionType"/> entities
/// into simplified <see cref="ScreenActionTypeDTO"/>s for use in API responses.

using System.Linq.Expressions;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Mappers;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="ScreenActionType"/> entities into <see cref="ScreenActionTypeDTO"/>s.
/// </summary>
public static class ScreenActionTypeMapper
{
    /// <summary>
    /// 👉 ✨ Returns an expression that maps a <see cref="ScreenActionType"/> to a <see cref="ScreenActionTypeDTO"/>.
    /// </summary>
    /// <returns>A compiled <see cref="Expression"/> that can be used in EF Core LINQ queries.</returns>
    public static Expression<Func<ScreenActionType, ScreenActionTypeDTO>> ToScreenActionTypeDto()
    {
        return type => new ScreenActionTypeDTO
        {
            Id = type.Id,                   // 🔑 Unique ID of the action type
            Name = type.Name,               // 🏷️ Label for the action type (e.g. "Click", "Submit")
            Description = type.Description  // 📝 Optional description for UI/tooltips
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Compiled expression allows direct use in EF Core LINQ queries for optimized SQL translation.
/// - 📦 DTO keeps only relevant fields — extend if needed for admin features.
/// - 🧠 Useful in dropdowns, filters, or for tagging actions with their type.
/// - ✨ Simple and flat DTO ensures it's safe to expose to public API consumers.
