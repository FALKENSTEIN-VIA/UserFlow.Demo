namespace UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Represents a company within the system.
/// </summary>
public class Company : BaseEntity
{
    /// <summary>
    /// 🏢 Name of the company.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📍 Address of the company.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// ☎️ Contact phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 👥 Users registered under this company (system users).
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// 👨‍💼 Employees assigned to this company (non-login records).
    /// </summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <summary>
    /// 🔢 Calculated or cached number of users in the company (optional).
    /// </summary>
    public int UserCount { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 🏢 Companies are the top-level tenants in the system.
/// - 🔗 Related to Users (login accounts) and Employees (descriptive entries).
/// - 🧠 Inherits audit and soft-delete fields from BaseEntity.
/// - 📊 `UserCount` may be filled during queries or for dashboard usage.
/// - ⚠️ Keep relationships consistent in configuration to avoid shadow properties or cascade errors.
