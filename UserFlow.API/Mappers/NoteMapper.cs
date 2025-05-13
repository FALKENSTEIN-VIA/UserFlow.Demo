/// @file NoteMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides a mapping expression from Note entities to NoteDTOs.
/// @details
/// Defines a compiled projection expression for transforming <see cref="Note"/> entities
/// into <see cref="NoteDTO"/> objects, including optional lookup fields like ProjectName and ScreenName.

using System.Linq.Expressions;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Mappers;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="Note"/> entities into <see cref="NoteDTO"/>s.
/// </summary>
public static class NoteMapper
{
    /// <summary>
    /// 👉 ✨ Returns a compiled expression that maps a <see cref="Note"/> to a <see cref="NoteDTO"/>.
    /// </summary>
    /// <returns>An EF Core-compatible <see cref="Expression"/> for Note-to-DTO mapping.</returns>
    public static Expression<Func<Note, NoteDTO>> ToNoteDto()
    {
        return note => new NoteDTO
        {
            Id = note.Id,                                         // 🔑 Note identifier
            Title = note.Title,                                   // 📝 Note title
            Content = note.Content,                               // 📄 Full note text
            UserId = note.UserId,                                 // 👤 Creator of the note
            CompanyId = note.CompanyId,                           // 🏢 Belongs to a company
            ProjectId = note.ProjectId ?? 0,                      // 📌 Related project (nullable fallback)
            ScreenId = note.ScreenId ?? 0,                        // 🖥️ Related screen (nullable fallback)
            ProjectName = note.Project != null
                ? note.Project.Name
                : string.Empty,                                   // 🏷️ Friendly name of the project (optional)
            ScreenName = note.Screen != null
                ? note.Screen.Name
                : string.Empty                                    // 📺 Friendly name of the screen (optional)
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Projection is implemented as an expression to ensure optimal SQL translation via EF Core.
/// - 🧩 Nullable navigation properties (Project, Screen) are handled gracefully with default fallback values.
/// - 💡 Use this mapper in LINQ queries where DTO projection and performance matter.
/// - 🧼 Avoids loading unnecessary entities when fields like ProjectName and ScreenName aren't used.
/// - 🔒 You can later adapt this to add access checks or user-specific visibility logic.
