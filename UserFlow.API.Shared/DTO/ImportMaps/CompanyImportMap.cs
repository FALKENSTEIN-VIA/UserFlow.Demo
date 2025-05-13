/// *****************************************************************************************
/// @file CompanyImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief CsvHelper mapping configuration for importing companies from CSV files.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO.ImportMaps;

/// <summary>
/// 📄 CSV import map for <see cref="CompanyImportDTO"/>.
/// </summary>
/// <remarks>
/// This class maps CSV column headers to the properties of <see cref="CompanyImportDTO"/>.
/// </remarks>
public sealed class CompanyImportMap : ClassMap<CompanyImportDTO>
{
    /// <summary>
    /// 🗺️ Configures the mapping between CSV columns and DTO fields.
    /// </summary>
    public CompanyImportMap()
    {
        // 🏷️ Maps the "Name" column in CSV to CompanyImportDTO.Name
        Map(m => m.Name).Name("Name");

        // 🏠 Maps the "Address" column in CSV to CompanyImportDTO.Address
        Map(m => m.Address).Name("Address");

        // ☎️ Maps the "PhoneNumber" column in CSV to CompanyImportDTO.PhoneNumber
        Map(m => m.PhoneNumber).Name("PhoneNumber");
    }
}

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - This map enables CsvHelper to deserialize CSV rows into CompanyImportDTO objects.
/// - The header names in the CSV must exactly match the values set via `.Name(...)`.
/// - Used in the CompanyController import endpoint to bind incoming data.
/// - Can be extended if more fields are added to CompanyImportDTO.
/// *****************************************************************************************
