/// *****************************************************************************************
/// @file ScreenActionTypeImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief CsvHelper mapping class for importing screen action types from CSV.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO.ImportMaps;

/// <summary>
/// 📄 CsvHelper mapping for <see cref="ScreenActionTypeImportDTO"/>.
/// </summary>
/// <remarks>
/// This class defines how columns in a CSV file map to properties in the <see cref="ScreenActionTypeImportDTO"/>.
/// It is used during the import of screen action types.
/// </remarks>
public class ScreenActionTypeImportMap : ClassMap<ScreenActionTypeImportDTO>
{
    /// <summary>
    /// 🛠️ Constructor to configure CSV column mappings.
    /// </summary>
    public ScreenActionTypeImportMap()
    {
        // 🏷️ Maps the "Name" column from the CSV to the Name property
        Map(x => x.Name).Name("Name");

        // 📝 Maps the "Description" column from the CSV to the Description property
        Map(x => x.Description).Name("Description");
    }
}

/// *****************************************************************************************
/// @remarks 🧩 Developer Notes:
/// - This mapping enables typed import of screen action types from CSV.
/// - Ensure the CSV file contains "Name" and "Description" headers.
/// - Used by endpoints that support bulk import of screen action types.
/// - Adjust if DTO properties change or additional columns are added.
/// *****************************************************************************************
