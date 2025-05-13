/// *****************************************************************************************
/// @file UserImportMap.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief DTO and CsvHelper mapping for importing users from a CSV file.
/// *****************************************************************************************

using CsvHelper.Configuration;

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 🧑‍💼 DTO used for importing basic user data from CSV files.
/// </summary>
/// <remarks>
/// Contains only the minimal fields required for creating user accounts via import.
/// </remarks>
public class UserImportDTO
{
    /// <summary>
    /// 🧾 Full name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 🗂️ CsvHelper map class to define column mappings for <see cref="UserImportDTO"/>.
/// </summary>
/// <remarks>
/// Allows flexible header names such as "Name" or "FullName", and "Email" or "EmailAddress".
/// </remarks>
public sealed class UserImportMap : ClassMap<UserImportDTO>
{
    /// <summary>
    /// 🔄 Constructor that configures column-to-property mappings.
    /// </summary>
    public UserImportMap()
    {
        // 🔁 Map both "Name" and "FullName" CSV headers to the Name property
        Map(m => m.Name).Name("Name", "FullName");

        // 🔁 Map both "Email" and "EmailAddress" headers to the Email property
        Map(m => m.Email).Name("Email", "EmailAddress");
    }
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - This class supports flexible CSV headers for name and email columns.
/// - Used in import scenarios where user data is provided from external systems.
/// - All fields are required for successful user creation or validation.
/// *****************************************************************************************
