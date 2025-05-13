/// *****************************************************************************************
/// @file EmployeeImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief CsvHelper mapping configuration for importing employees from CSV files.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO.ImportMaps;

/// <summary>
/// 📄 CSV import map for <see cref="EmployeeImportDTO"/>.
/// </summary>
/// <remarks>
/// This map binds CSV headers to the corresponding properties in <see cref="EmployeeImportDTO"/>.
/// </remarks>
public sealed class EmployeeImportMap : ClassMap<EmployeeImportDTO>
{
    /// <summary>
    /// 🗺️ Configures the mapping between CSV columns and EmployeeImportDTO properties.
    /// </summary>
    public EmployeeImportMap()
    {
        // 🧑 Maps the "Name" column to EmployeeImportDTO.Name
        Map(m => m.Name).Name("Name");

        // 📧 Maps the "Email" column to EmployeeImportDTO.Email
        Map(m => m.Email).Name("Email");

        // 🏷️ Maps the "Role" column to EmployeeImportDTO.Role
        Map(m => m.Role).Name("Role");

        // 🏢 Maps the "CompanyId" column to EmployeeImportDTO.CompanyId
        Map(m => m.CompanyId).Name("CompanyId");

        // 🔐 Maps the "UserId" column to EmployeeImportDTO.UserId
        Map(m => m.UserId).Name("UserId");
    }
}

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - Used in the EmployeeController's Import endpoint.
/// - Enables CsvHelper to deserialize rows into EmployeeImportDTOs.
/// - CSV headers must match exactly: "Name", "Email", "Role", "CompanyId", "UserId".
/// - Extend if additional fields are introduced in the DTO.
/// *****************************************************************************************
