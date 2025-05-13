/// *****************************************************************************************
/// @file NoteImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief CSV import map for NoteImportDTO using CsvHelper.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO.ImportMaps;

/// <summary>
/// 📄 CSV mapping configuration for importing note data.
/// </summary>
/// <remarks>
/// This class maps CSV column headers to properties in the <see cref="NoteImportDTO"/>.
/// Used during import to parse CSV rows into DTOs.
/// </remarks>
public class NoteImportMap : ClassMap<NoteImportDTO>
{
    /// <summary>
    /// 🛠️ Constructor that defines CSV-to-property mappings.
    /// </summary>
    public NoteImportMap()
    {
        // 📝 Map the "Title" column to the Title property
        Map(n => n.Title).Name("Title");

        // 📝 Map the "Content" column to the Content property
        Map(n => n.Content).Name("Content");

        // 🔗 Map the "ProjectId" column to the ProjectId property
        Map(n => n.ProjectId).Name("ProjectId");

        // 🔗 Map the "ScreenId" column to the ScreenId property
        Map(n => n.ScreenId).Name("ScreenId");

        // 🔗 Map the "ScreenActionId" column to the ScreenActionId property
        Map(n => n.ScreenActionId).Name("ScreenActionId");
    }
}

/// *****************************************************************************************
/// @remarks 🧩 Developer Notes:
/// - Used by CsvHelper to parse each row in a CSV file into a NoteImportDTO object.
/// - All columns must match exactly (case-sensitive) with the names defined here.
/// - Optional columns (ScreenId, ScreenActionId) may be null.
/// - Add additional mappings here if the import schema changes.
/// *****************************************************************************************
