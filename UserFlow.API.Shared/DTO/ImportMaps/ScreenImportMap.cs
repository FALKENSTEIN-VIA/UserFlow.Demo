/// *****************************************************************************************
/// @file ScreenImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief CsvHelper mapping class for importing screens from a CSV file.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO.ImportMaps;

/// <summary>
/// 🖥️ CsvHelper mapping for <see cref="ScreenImportDTO"/> used in screen import functionality.
/// </summary>
/// <remarks>
/// This class configures how CSV columns are mapped to the <see cref="ScreenImportDTO"/> properties.
/// It is utilized by the import logic in the API to convert raw CSV data into structured DTOs.
/// </remarks>
public class ScreenImportMap : ClassMap<ScreenImportDTO>
{
    /// <summary>
    /// 🛠️ Constructor that sets up the CSV-to-property mappings.
    /// </summary>
    public ScreenImportMap()
    {
        // 🏷️ Maps the "Name" column in the CSV to the Name property in the DTO
        Map(x => x.Name).Name("Name");

        // 🆔 Maps the "Identifier" column to the screen identifier field
        Map(x => x.Identifier).Name("Identifier");

        // 📝 Maps the "Description" column to the screen description
        Map(x => x.Description).Name("Description");

        // 🧩 Maps the "Type" column, which defines the screen's category or purpose
        Map(x => x.Type).Name("Type");

        // 🔗 Maps the "ProjectId" column, linking the screen to a specific project
        Map(x => x.ProjectId).Name("ProjectId");
    }
}

/// *****************************************************************************************
/// @remarks 🧩 Developer Notes:
/// - This mapping is used in CSV import operations for screens.
/// - It assumes the presence of headers: "Name", "Identifier", "Description", "Type", "ProjectId".
/// - All properties are mapped directly to the ScreenImportDTO.
/// - Ensure header names match exactly, otherwise CsvHelper will skip the column.
/// *****************************************************************************************
