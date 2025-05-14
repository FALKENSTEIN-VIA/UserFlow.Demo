/// *****************************************************************************************
/// @file EmployeeDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Contains DTOs for managing employee data (create, update, import, bulk).
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

#region 👤 EmployeeDTO

/// <summary>
/// 👤 Represents a system employee with optional user and company references.
/// </summary>
public class EmployeeDTO : BaseDTO
{
    /// <summary>
    /// 🧑 Full name of the employee.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email address of the employee.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🛡️ Role assigned to the employee (e.g., User, Manager, Admin).
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// 🏢 Optional reference to the owning company.
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// 🏷️ Optional company name (for display).
    /// </summary>
    public string? CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 👤 Optional reference to the related Identity User.
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 🏢 Company details (if included).
    /// </summary>
    public CompanyDTO? Company { get; set; }

    /// <summary>
    /// 🔑 Optional password (used during internal creation scenarios).
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

#endregion

#region 🆕 EmployeeCreateDTO

/// <summary>
/// 🆕 DTO for creating a new employee.
/// </summary>
public class EmployeeCreateDTO
{
    /// <summary>
    /// 🧑 Name of the employee.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email of the employee.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🛡️ Role to assign (default: User).
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// 🏢 Optional company ID.
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// 👤 ID of the Identity user creating this employee.
    /// </summary>
    public long UserId { get; set; }
}

#endregion

#region ✏️ EmployeeUpdateDTO

/// <summary>
/// ✏️ DTO for updating an existing employee.
/// </summary>
public class EmployeeUpdateDTO
{
    /// <summary>
    /// 🆔 ID of the employee.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🧑 Updated name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Updated email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🛡️ Updated role (if applicable).
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// 🏢 Updated company reference.
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// 👤 Updated Identity user reference.
    /// </summary>
    public long? UserId { get; set; }
}

#endregion

#region 📥 EmployeeImportDTO

/// <summary>
/// 📥 DTO used for importing employee data from external sources (CSV, Excel).
/// </summary>
public class EmployeeImportDTO
{
    /// <summary>
    /// 🧑 Name of the imported employee.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Email of the imported employee.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🛡️ Role of the imported employee.
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// 🏢 Optional company ID.
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// 👤 Optional Identity user ID.
    /// </summary>
    public long? UserId { get; set; }
}

#endregion

#region 📦 BulkEmployeeCreateDTO

/// <summary>
/// 📦 DTO for creating multiple employees in one request.
/// </summary>
public class BulkEmployeeCreateDTO
{
    /// <summary>
    /// 📋 List of employees to create.
    /// </summary>
    public List<EmployeeCreateDTO> Employees { get; set; } = new();
}

#endregion

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - `EmployeeDTO` inherits from `BaseDTO` for metadata tracking.
/// - Password is used internally and should not be exposed to clients directly.
/// - Role defaults to "User", but can be elevated to Manager/Admin by authorized users.
/// - All fields follow the conventions used in other DTOs (consistency across entities).
/// - DTOs are serialized to/from JSON during API interaction.
/// *****************************************************************************************
