/// *****************************************************************************************
/// @file ProjectImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief CSV import map for ProjectImportDTO using CsvHelper.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO.ImportMaps;

/// <summary>
/// 📄 CSV mapping configuration for importing project data.
/// </summary>
/// <remarks>
/// This class maps CSV column headers to properties in the <see cref="ProjectImportDTO"/>.
/// It is used by CsvHelper during the import process.
/// </remarks>
public class ProjectImportMap : ClassMap<ProjectImportDTO>
{
    /// <summary>
    /// 🛠️ Constructor defining the column-to-property mappings.
    /// </summary>
    public ProjectImportMap()
    {
        // 🏷️ Map the "Name" column to the Name property
        Map(p => p.Name).Name("Name");

        // 📝 Map the "Description" column to the Description property
        Map(p => p.Description).Name("Description");

        // 📤 Map the "IsShared" column to the IsShared property
        Map(p => p.IsShared).Name("IsShared");
    }
}

/// *****************************************************************************************
/// @remarks 🧩 Developer Notes:
/// - This mapping ensures that CSV headers are correctly mapped to DTO properties.
/// - Used in the import pipeline for projects.
/// - If new fields are added to ProjectImportDTO, extend this mapping accordingly.
/// - Ensure column names in the CSV exactly match the names specified here (case-sensitive).
/// *****************************************************************************************
