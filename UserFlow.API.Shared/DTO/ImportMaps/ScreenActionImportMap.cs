/// *****************************************************************************************
/// @file ScreenActionImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief CsvHelper mapping class for importing screen actions via CSV.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO.ImportMaps;

/// <summary>
/// 📄 CsvHelper mapping for <see cref="ScreenActionImportDTO"/>.
/// </summary>
/// <remarks>
/// Defines how CSV columns map to properties in <see cref="ScreenActionImportDTO"/>.
/// This is used during bulk CSV import of screen actions.
/// </remarks>
public class ScreenActionImportMap : ClassMap<ScreenActionImportDTO>
{
    /// <summary>
    /// 🛠️ Constructor that defines the column-property mappings.
    /// </summary>
    public ScreenActionImportMap()
    {
        // 🏷️ Map "Name" column to Name property
        Map(x => x.Name).Name("Name");

        // 📝 Map "EventDescription" column to EventDescription property
        Map(x => x.EventDescription).Name("EventDescription");

        // 🔲 Map "EventAreaDefined" column to EventAreaDefined property
        Map(x => x.EventAreaDefined).Name("EventAreaDefined");

        // 🧭 Map coordinates for event area (if defined)
        Map(x => x.EventX1).Name("EventX1");
        Map(x => x.EventY1).Name("EventY1");
        Map(x => x.EventX2).Name("EventX2");
        Map(x => x.EventY2).Name("EventY2");

        // 🔢 Sort index for ordering
        Map(x => x.SortIndex).Name("SortIndex");

        // 🔗 Map relational IDs
        Map(x => x.ScreenId).Name("ScreenId");
        Map(x => x.ScreenActionTypeId).Name("ScreenActionTypeId");
        Map(x => x.SuccessorScreenId).Name("SuccessorScreenId");
        Map(x => x.ProjectId).Name("ProjectId");
    }
}

/// *****************************************************************************************
/// @remarks 🧩 Developer Notes:
/// - Used in screen action import endpoints to correctly parse CSV files.
/// - Ensure the CSV header names exactly match the names defined here.
/// - This mapping allows robust and typed CSV parsing using CsvHelper.
/// - Adjust this mapping if the import DTO changes (e.g., new fields).
/// *****************************************************************************************
