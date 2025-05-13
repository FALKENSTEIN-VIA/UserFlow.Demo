/// *****************************************************************************************
/// @file CompanyDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Contains DTOs for company management, including creation, update, registration, and import.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 🏢 CompanyDTO

/// <summary>
/// 🏢 Represents a company including related metadata and user list.
/// </summary>
public class CompanyDTO : BaseDTO
{
    /// <summary>
    /// 🏷️ Name of the company.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🏠 Physical address of the company.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// ☎️ Contact phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 👥 Number of users associated with this company.
    /// </summary>
    public int? UserCount { get; set; }

    /// <summary>
    /// 👤 Optional list of users that belong to this company.
    /// </summary>
    public List<UserDTO>? Users { get; set; }

    /// <summary>
    /// 🧾 Returns a readable string representation of the company.
    /// </summary>
    public override string ToString() => $"{Id} - {Name}";
}

#endregion

#region 🆕 CompanyCreateDTO

/// <summary>
/// 📦 DTO for creating a new company.
/// </summary>
public class CompanyCreateDTO
{
    /// <summary>
    /// 🏷️ Name of the new company.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🏠 Address of the new company.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// ☎️ Contact phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
}

#endregion

#region ✏️ CompanyUpdateDTO

/// <summary>
/// ✏️ DTO for updating an existing company.
/// </summary>
public class CompanyUpdateDTO
{
    /// <summary>
    /// 🆔 ID of the company to update.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🏷️ Updated name of the company.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🏠 Updated address of the company.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// ☎️ Updated contact phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
}

#endregion

#region 📝 CompanyRegisterDTO

/// <summary>
/// 📝 DTO for registering a company along with its first admin user.
/// </summary>
public class CompanyRegisterDTO
{
    /// <summary>
    /// 🏷️ Name of the new company.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 🏠 Address of the new company.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// ☎️ Phone number of the new company.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email of the initial admin user.
    /// </summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// 🔑 Password of the initial admin user.
    /// </summary>
    public string AdminPassword { get; set; } = string.Empty;
}

#endregion

#region 📥 CompanyImportDTO

/// <summary>
/// 📥 DTO for importing companies via CSV or Excel.
/// </summary>
public class CompanyImportDTO
{
    /// <summary>
    /// 🏷️ Name of the imported company.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 🏠 Optional address of the company.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// ☎️ Optional phone number of the company.
    /// </summary>
    public string? PhoneNumber { get; set; }
}

#endregion

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - `CompanyDTO` inherits from `BaseDTO` for consistent metadata (Id, CreatedAt, etc.).
/// - `CompanyRegisterDTO` is used in the public endpoint to register a company + admin user.
/// - `CompanyImportDTO` is used during CSV import (e.g., via CsvHelper).
/// - Nullable fields in import DTOs allow partial import with optional data.
/// - DTOs are used in both API requests and responses to decouple internal models from clients.
/// *****************************************************************************************
